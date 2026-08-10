from __future__ import annotations

import hashlib
import logging
import os
import random
import time
from pathlib import Path
from urllib.error import HTTPError, URLError

from .models import DownloadState, DownloadTask, RemoteInfo
from .network import analyze, open_stream
from .persistence import DownloadRepository


LOG = logging.getLogger("idm")
CHUNK_SIZE = 1024 * 1024
CHECKPOINT_BYTES = 4 * CHUNK_SIZE
OVERLAP = 64 * 1024


class RemoteFileChanged(RuntimeError):
    pass


class DownloadEngine:
    def __init__(self, repository: DownloadRepository, allow_private: bool = False) -> None:
        self.repository = repository
        self.allow_private = allow_private
        self.stop_requested = False

    def add(self, url: str, destination_dir: Path, filename: str | None = None) -> DownloadTask:
        info = analyze(url, self.allow_private)
        destination_dir.mkdir(parents=True, exist_ok=True)
        destination = (destination_dir / (filename or info.filename)).resolve()
        if destination.exists() or destination.with_name(destination.name + ".download").exists():
            raise FileExistsError(f"La destination existe déjà: {destination}")
        return self.repository.create(info, destination)

    def run(self, task_id: int, max_attempts: int = 6) -> DownloadTask:
        self.stop_requested = False
        task = self.repository.get(task_id)
        task.temporary_path.parent.mkdir(parents=True, exist_ok=True)
        disk_bytes = task.temporary_path.stat().st_size if task.temporary_path.exists() else 0
        safe_bytes = min(task.confirmed_bytes, disk_bytes)
        if disk_bytes != safe_bytes:
            with task.temporary_path.open("r+b") as output:
                output.truncate(safe_bytes)
        self.repository.update(task_id, state=DownloadState.ANALYZING, confirmed_bytes=safe_bytes, error=None)
        info = analyze(task.original_url, self.allow_private)
        try:
            self._verify_identity(task, info)
        except RemoteFileChanged as error:
            self.repository.update(task_id, state=DownloadState.REMOTE_CHANGED, error=str(error))
            return self.repository.get(task_id)
        self.repository.update(task_id, final_url=info.final_url, total_size=info.total_size,
                               etag=info.etag, last_modified=info.last_modified)

        attempt = task.attempts
        while attempt < max_attempts and not self.stop_requested:
            try:
                self._transfer(self.repository.get(task_id))
                return self.repository.get(task_id)
            except RemoteFileChanged as error:
                self.repository.update(task_id, state=DownloadState.REMOTE_CHANGED, error=str(error))
                return self.repository.get(task_id)
            except (HTTPError, URLError, TimeoutError, ConnectionError, OSError) as error:
                attempt += 1
                state = DownloadState.TEMPORARY_FAILURE if attempt >= max_attempts else DownloadState.RECONNECTING
                self.repository.update(task_id, state=state, attempts=attempt, error=self._safe_error(error))
                if attempt < max_attempts and not self.stop_requested:
                    delay = min(60.0, (2 ** attempt)) + random.uniform(0, 0.5)
                    LOG.warning("Tentative %s échouée; reprise dans %.1fs", attempt, delay)
                    time.sleep(delay)

        if self.stop_requested:
            self.repository.update(task_id, state=DownloadState.PAUSED, error=None)
        return self.repository.get(task_id)

    def _transfer(self, task: DownloadTask) -> None:
        offset = task.confirmed_bytes
        request_offset = max(0, offset - OVERLAP) if offset else 0
        with open_stream(task.final_url, request_offset, task.etag, task.last_modified) as response:
            if request_offset:
                expected_prefix = f"bytes {request_offset}-"
                if response.status != 206 or not response.headers.get("Content-Range", "").startswith(expected_prefix):
                    raise RemoteFileChanged("Le serveur n’a pas confirmé la plage de reprise demandée")
            elif response.status not in {200, 206}:
                raise HTTPError(task.final_url, response.status, "Réponse inattendue", response.headers, None)

            mode = "r+b" if task.temporary_path.exists() else "w+b"
            with task.temporary_path.open(mode) as output:
                if request_offset < offset:
                    overlap_size = offset - request_offset
                    received = response.read(overlap_size)
                    output.seek(request_offset)
                    existing = output.read(overlap_size)
                    if received != existing:
                        raise RemoteFileChanged("La zone de recouvrement diffère du fichier local")
                output.seek(offset)
                confirmed = offset
                checkpoint = offset
                self.repository.update(task.id, state=DownloadState.DOWNLOADING, error=None)
                while not self.stop_requested:
                    block = response.read(CHUNK_SIZE)
                    if not block:
                        break
                    output.write(block)
                    confirmed += len(block)
                    if confirmed - checkpoint >= CHECKPOINT_BYTES:
                        output.flush()
                        os.fsync(output.fileno())
                        self.repository.update(task.id, confirmed_bytes=confirmed)
                        checkpoint = confirmed
                output.flush()
                os.fsync(output.fileno())
                self.repository.update(task.id, confirmed_bytes=confirmed)

        if self.stop_requested:
            return
        task = self.repository.get(task.id)
        if task.total_size is not None and task.confirmed_bytes != task.total_size:
            raise ConnectionError(
                f"Flux incomplet: {task.confirmed_bytes} octets reçus sur {task.total_size}"
            )
        self._finalize(task)

    def _finalize(self, task: DownloadTask) -> None:
        self.repository.update(task.id, state=DownloadState.VERIFYING)
        actual = task.temporary_path.stat().st_size
        if task.total_size is not None and actual != task.total_size:
            raise OSError(f"Taille finale incorrecte: {actual} au lieu de {task.total_size}")
        # Reading the whole file catches late I/O errors and produces a useful local fingerprint.
        digest = hashlib.sha256()
        with task.temporary_path.open("rb") as source:
            for block in iter(lambda: source.read(CHUNK_SIZE), b""):
                digest.update(block)
        LOG.info("SHA-256 local: %s", digest.hexdigest())
        self.repository.update(task.id, state=DownloadState.FINALIZING)
        if task.destination.exists():
            raise FileExistsError(f"Le fichier final existe déjà: {task.destination}")
        os.replace(task.temporary_path, task.destination)
        self.repository.update(task.id, state=DownloadState.COMPLETED, error=None)

    @staticmethod
    def _verify_identity(task: DownloadTask, info: RemoteInfo) -> None:
        if task.total_size is not None and info.total_size is not None and task.total_size != info.total_size:
            raise RemoteFileChanged("La taille du fichier distant a changé")
        if task.etag and info.etag and task.etag != info.etag:
            raise RemoteFileChanged("L’ETag du fichier distant a changé")
        if not task.etag and task.last_modified and info.last_modified != task.last_modified:
            raise RemoteFileChanged("La date de modification distante a changé")

    @staticmethod
    def _safe_error(error: BaseException) -> str:
        # Avoid logging complete signed URLs or credentials from exception messages.
        if isinstance(error, HTTPError):
            return f"HTTP-{error.code}: le serveur a refusé la requête"
        return f"{type(error).__name__}: {str(error)[:300]}"
