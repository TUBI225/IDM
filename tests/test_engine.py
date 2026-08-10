from __future__ import annotations

import contextlib
import hashlib
import re
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from idm.engine import DownloadEngine
from idm.models import DownloadState
from idm.persistence import DownloadRepository


class RangeHandler(BaseHTTPRequestHandler):
    content = b""
    etag = '"test-v1"'

    def do_GET(self) -> None:
        data = type(self).content
        start = 0
        range_header = self.headers.get("Range")
        if range_header:
            match = re.fullmatch(r"bytes=(\d+)-(\d*)", range_header)
            if match is None:
                self.send_error(416)
                return
            start = int(match.group(1))
            end = int(match.group(2)) if match.group(2) else len(data) - 1
            if start >= len(data):
                self.send_response(416)
                self.send_header("Content-Range", f"bytes */{len(data)}")
                self.end_headers()
                return
            end = min(end, len(data) - 1)
            body = data[start:end + 1]
            self.send_response(206)
            self.send_header("Content-Range", f"bytes {start}-{end}/{len(data)}")
        else:
            body = data
            self.send_response(200)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Content-Type", "application/octet-stream")
        self.send_header("Content-Disposition", 'attachment; filename="fixture.bin"')
        self.send_header("ETag", type(self).etag)
        self.send_header("Accept-Ranges", "bytes")
        self.end_headers()
        with contextlib.suppress(BrokenPipeError, ConnectionResetError):
            self.wfile.write(body)

    def log_message(self, _format: str, *args: object) -> None:
        pass


class EngineIntegrationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        RangeHandler.content = bytes(range(256)) * (64 * 1024)
        cls.server = ThreadingHTTPServer(("127.0.0.1", 0), RangeHandler)
        cls.thread = threading.Thread(target=cls.server.serve_forever, daemon=True)
        cls.thread.start()
        cls.url = f"http://127.0.0.1:{cls.server.server_port}/fixture.bin"

    @classmethod
    def tearDownClass(cls) -> None:
        cls.server.shutdown()
        cls.server.server_close()

    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.repository = DownloadRepository(self.root / "state.sqlite3")
        self.engine = DownloadEngine(self.repository, allow_private=True)

    def tearDown(self) -> None:
        RangeHandler.etag = '"test-v1"'
        self.temp.cleanup()

    def test_full_download_is_verified_and_atomically_finalized(self) -> None:
        task = self.engine.add(self.url, self.root / "downloads")
        result = self.engine.run(task.id, max_attempts=1)
        self.assertEqual(DownloadState.COMPLETED, result.state)
        self.assertFalse(result.temporary_path.exists())
        self.assertEqual(hashlib.sha256(RangeHandler.content).digest(),
                         hashlib.sha256(result.destination.read_bytes()).digest())

    def test_restart_resumes_from_confirmed_disk_position(self) -> None:
        task = self.engine.add(self.url, self.root / "downloads")
        confirmed = 5 * 1024 * 1024
        task.temporary_path.parent.mkdir(parents=True, exist_ok=True)
        task.temporary_path.write_bytes(RangeHandler.content[:confirmed])
        self.repository.update(task.id, confirmed_bytes=confirmed, state=DownloadState.PAUSED)

        restarted_engine = DownloadEngine(
            DownloadRepository(self.root / "state.sqlite3"), allow_private=True
        )
        result = restarted_engine.run(task.id, max_attempts=1)
        self.assertEqual(DownloadState.COMPLETED, result.state)
        self.assertEqual(RangeHandler.content, result.destination.read_bytes())

    def test_changed_remote_file_is_never_mixed(self) -> None:
        task = self.engine.add(self.url, self.root / "downloads")
        confirmed = 1024 * 1024
        task.temporary_path.parent.mkdir(parents=True, exist_ok=True)
        task.temporary_path.write_bytes(RangeHandler.content[:confirmed])
        self.repository.update(task.id, confirmed_bytes=confirmed)
        RangeHandler.etag = '"test-v2"'

        result = self.engine.run(task.id, max_attempts=1)
        self.assertEqual(DownloadState.REMOTE_CHANGED, result.state)
        self.assertEqual(confirmed, task.temporary_path.stat().st_size)


if __name__ == "__main__":
    unittest.main()
