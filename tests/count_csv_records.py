#!/usr/bin/env python3
"""Count CSV records in plain, .gz, or .bz2 files (handles multiline fields)."""

from __future__ import annotations

import argparse
import bz2
import csv
import gzip
import sys
from pathlib import Path
from typing import IO, Iterable, Iterator, TextIO


def open_text(path: str) -> TextIO:
    if path == "-":
        return sys.stdin

    suffix = Path(path).suffix.lower()
    if suffix == ".gz":
        return gzip.open(path, mode="rt", encoding="utf-8", newline="")
    if suffix in {".bz2", ".bz"}:
        return bz2.open(path, mode="rt", encoding="utf-8", newline="")
    return open(path, mode="rt", encoding="utf-8", newline="")


def count_records(rows: Iterable[list[str]], skip_header: bool) -> int:
    iterator = iter(rows)
    if skip_header:
        next(iterator, None)
    return sum(1 for _ in iterator)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Count CSV records in plain, .gz, or .bz2 files (handles multiline fields)."
    )
    parser.add_argument("path", help="CSV file path (.csv, .gz, .bz2) or '-' for stdin")
    parser.add_argument(
        "--skip-header",
        action="store_true",
        help="Skip the first row before counting",
    )
    return parser


def main(argv: list[str]) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    try:
        display_name = "<stdin>" if args.path == "-" else args.path
        print(f"{display_name}: ", end="", flush=True)
        with open_text(args.path) as handle:
            reader = csv.reader(handle)
            total = count_records(reader, args.skip_header)
    except FileNotFoundError:
        parser.error(f"File not found: {args.path}")
    except (OSError, csv.Error) as exc:
        parser.error(str(exc))

    print(total)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
