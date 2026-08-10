from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum
from pathlib import Path


class DownloadState(StrEnum):
    NEW = "NOUVEAU"
    ANALYZING = "ANALYSE"
    PREPARING = "PREPARATION"
    DOWNLOADING = "TELECHARGEMENT"
    PAUSED = "EN_PAUSE"
    RECONNECTING = "RECONNEXION"
    VERIFYING = "VERIFICATION"
    FINALIZING = "FINALISATION"
    COMPLETED = "TERMINE"
    TEMPORARY_FAILURE = "ECHEC_TEMPORAIRE"
    PERMANENT_FAILURE = "ECHEC_PERMANENT"
    REMOTE_CHANGED = "FICHIER_DISTANT_MODIFIE"


@dataclass(slots=True)
class RemoteInfo:
    original_url: str
    final_url: str
    filename: str
    total_size: int | None
    mime_type: str | None
    etag: str | None
    last_modified: str | None
    supports_ranges: bool


@dataclass(slots=True)
class DownloadTask:
    id: int
    original_url: str
    final_url: str
    destination: Path
    temporary_path: Path
    state: DownloadState
    total_size: int | None
    confirmed_bytes: int
    etag: str | None
    last_modified: str | None
    attempts: int
    error: str | None

