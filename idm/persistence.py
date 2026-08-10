from __future__ import annotations

import sqlite3
from contextlib import contextmanager
from pathlib import Path
from typing import Iterator

from .models import DownloadState, DownloadTask, RemoteInfo


SCHEMA = """
CREATE TABLE IF NOT EXISTS downloads (
    id INTEGER PRIMARY KEY,
    original_url TEXT NOT NULL,
    final_url TEXT NOT NULL,
    destination TEXT NOT NULL UNIQUE,
    temporary_path TEXT NOT NULL,
    state TEXT NOT NULL,
    total_size INTEGER,
    confirmed_bytes INTEGER NOT NULL DEFAULT 0,
    etag TEXT,
    last_modified TEXT,
    attempts INTEGER NOT NULL DEFAULT 0,
    error TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_downloads_state ON downloads(state);
"""


class DownloadRepository:
    def __init__(self, database: Path) -> None:
        self.database = database
        database.parent.mkdir(parents=True, exist_ok=True)
        with self._connect() as connection:
            connection.executescript(SCHEMA)

    @contextmanager
    def _connect(self) -> Iterator[sqlite3.Connection]:
        connection = sqlite3.connect(self.database)
        connection.row_factory = sqlite3.Row
        try:
            yield connection
            connection.commit()
        finally:
            connection.close()

    def create(self, info: RemoteInfo, destination: Path) -> DownloadTask:
        temporary = destination.with_name(destination.name + ".download")
        with self._connect() as connection:
            cursor = connection.execute(
                """INSERT INTO downloads
                (original_url, final_url, destination, temporary_path, state,
                 total_size, etag, last_modified)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (info.original_url, info.final_url, str(destination), str(temporary),
                 DownloadState.NEW, info.total_size, info.etag, info.last_modified),
            )
            task_id = int(cursor.lastrowid)
        return self.get(task_id)

    def get(self, task_id: int) -> DownloadTask:
        with self._connect() as connection:
            row = connection.execute("SELECT * FROM downloads WHERE id = ?", (task_id,)).fetchone()
        if row is None:
            raise KeyError(f"Téléchargement {task_id} introuvable")
        return self._to_task(row)

    def list(self) -> list[DownloadTask]:
        with self._connect() as connection:
            rows = connection.execute("SELECT * FROM downloads ORDER BY id").fetchall()
        return [self._to_task(row) for row in rows]

    def update(self, task_id: int, **values: object) -> None:
        if not values:
            return
        allowed = {"final_url", "state", "total_size", "confirmed_bytes", "etag",
                   "last_modified", "attempts", "error"}
        unknown = set(values) - allowed
        if unknown:
            raise ValueError(f"Champs interdits: {unknown}")
        if "state" in values and isinstance(values["state"], DownloadState):
            values["state"] = values["state"].value
        assignments = ", ".join(f"{key} = ?" for key in values)
        params = [*values.values(), task_id]
        with self._connect() as connection:
            connection.execute(
                f"UPDATE downloads SET {assignments}, updated_at=CURRENT_TIMESTAMP WHERE id = ?",
                params,
            )

    @staticmethod
    def _to_task(row: sqlite3.Row) -> DownloadTask:
        return DownloadTask(
            id=row["id"], original_url=row["original_url"], final_url=row["final_url"],
            destination=Path(row["destination"]), temporary_path=Path(row["temporary_path"]),
            state=DownloadState(row["state"]), total_size=row["total_size"],
            confirmed_bytes=row["confirmed_bytes"], etag=row["etag"],
            last_modified=row["last_modified"], attempts=row["attempts"], error=row["error"],
        )

