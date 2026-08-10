from __future__ import annotations

import argparse
import logging
import signal
from pathlib import Path

from .engine import DownloadEngine
from .models import DownloadTask
from .persistence import DownloadRepository


def _format(task: DownloadTask) -> str:
    total = str(task.total_size) if task.total_size is not None else "inconnue"
    return f"#{task.id} {task.state.value} — {task.confirmed_bytes}/{total} octets — {task.destination}"


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(prog="idm", description="Moteur HTTP fiable et reprenable")
    result.add_argument("--data-dir", type=Path, default=Path(".idm-data"))
    result.add_argument("--allow-private", action="store_true", help="Autoriser les serveurs locaux (tests)")
    sub = result.add_subparsers(dest="command", required=True)
    add = sub.add_parser("add", help="Analyser et ajouter un téléchargement")
    add.add_argument("url")
    add.add_argument("--output", type=Path, default=Path("downloads"))
    add.add_argument("--name")
    run = sub.add_parser("run", help="Démarrer ou reprendre un téléchargement")
    run.add_argument("id", type=int)
    run.add_argument("--attempts", type=int, default=6)
    sub.add_parser("list", help="Afficher les téléchargements")
    return result


def main(argv: list[str] | None = None) -> int:
    args = parser().parse_args(argv)
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    repository = DownloadRepository(args.data_dir / "downloads.sqlite3")
    engine = DownloadEngine(repository, allow_private=args.allow_private)
    if args.command == "add":
        task = engine.add(args.url, args.output, args.name)
        print(_format(task))
        return 0
    if args.command == "list":
        for task in repository.list():
            print(_format(task))
        return 0

    def request_stop(_signum: int, _frame: object) -> None:
        print("\nPause sûre demandée…")
        engine.stop_requested = True

    signal.signal(signal.SIGINT, request_stop)
    task = engine.run(args.id, args.attempts)
    print(_format(task))
    if task.error:
        print(task.error)
    return 0 if task.state.value in {"TERMINE", "EN_PAUSE"} else 1


if __name__ == "__main__":
    raise SystemExit(main())

