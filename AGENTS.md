# Repository Guidelines

## Python packaging tools

- Use `uv` for Python package management
- Use the built-in `uv build` for building packages
- If legacy packaging is used, like `setup.cfg` or `requirements.txt`, convert it to
  `pyproject.toml` using `uv add -r requirements.txt`

## Project Structure & Module Organization
- `discogsparser.py` and `model.py` implement the legacy SAX-based importer used by integration scripts; respect their tab-indented style when editing.
- `parsers/` defines entity handlers, while `exporters/` and `tools/` house adapters and CLI utilities for PostgreSQL, MongoDB, and CouchDB flows.
- `speedup/` contains the newer CSV-first pipeline (with `postgresql/` and `mysql/` helpers); treat it as the preferred path for high-volume imports.
- SQL helpers live beside the root `.sql` files, and `tests/` hosts pytest suites plus gzipped fixtures under `tests/samples/`.

## Build, Test, and Development Commands
- Use `uv run` for executing all Python based commands
- Install legacy flow dependencies: `uv add -r requirements.txt`.
- Install the accelerated stack: `uv add -r speedup/requirements.txt`.
- Parse a dump locally: `uv run python discogsparser.py -d 20240101 -o json`.
- Generate compressed CSVs with progress: `python3 speedup/exporter.py --bz2 --export release dump-dir out/csv`.
- Run the suite: `uv run python -m pytest`.

## Coding Style & Naming Conventions
- Python modules target 3.x while retaining 2.7 compatibility; keep imports grouped stdlib, third party, local.
- Legacy files mix tabs and 4-space blocks; match the surrounding indentation (see `setup.cfg` ignoring `W191`).
- Prefer descriptive `snake_case` for functions and attributes, `TitleCase` for exporter classes, and uppercase for module-level constants.
- Use UTF-8 literals (`u"…"`) where present and avoid f-strings in codepaths still exercised on 2.7.

## Testing Guidelines
- pytest discovers any `test_*.py`; new fixtures belong under `tests/samples/` to keep real data small.
- Cover new parser branches and exporter behaviours; mock filesystem/database calls when fixtures are insufficient.
- Run `uv run python -m pytest` before submitting and note relevant record counts or CLI output in the PR description.

## Commit & Pull Request Guidelines
- Follow the prevailing concise, imperative subject style (`Fix Mongo exporter path`, `Update README.md`).
- Keep commits focused; separate SQL migrations, Python changes, and data files so reviewers can diff quickly.
- PRs should explain the import scenario exercised, list verification commands, and link issues or tickets.
- Attach screenshots or logs when altering CLI progress output, schema tooling, or long-running scripts.

## Data Import Workflow Tips
- Verify new dumps first: `sha256sum -c discogs_*_CHECKSUM.txt`.
- After CSV generation, apply the relevant SQL scripts (`create_tables.sql`, `create_indexes.sql`, or `speedup/postgresql/sql/*.sql`) as part of validation.
