# Copilot Instructions

## Scope
This repository contains a Python-based exporter for Discogs XML dumps, plus an experimental .NET implementation under `alternatives/dotnet`.

## General guidance
- Keep diffs minimal and targeted; avoid large refactors unless explicitly requested.
- Follow existing patterns and naming conventions in the touched module.
- Respect the 120-character line limit from `setup.cfg`.
- Do not add or modify large binary/sample data in the repo.

## Python specifics
- CLI is defined in `run.py` via `docopt`; preserve argument semantics and help text.
- Prefer standard library usage consistent with the codebase (e.g., `os`, `pathlib`, `csv`).
- Tests are run with `python -m pytest` (deps in `requirements-dev.txt`).

### Code Review
- Ensure tests pass before merging.
- Require type annotations for new code.

## .NET specifics (if editing)
- Changes should stay within `alternatives/dotnet` and follow its README.

