# Agent Instructions

## Project summary
`discogs-xml2db` converts Discogs XML dumps into CSV for database import. The primary implementation is Python; there is an experimental .NET parser under `alternatives/dotnet`.

## Key paths
- `discogsxml2db/`: core Python exporter code
- `run.py`: CLI entrypoint (docopt)
- `tests/`: pytest suite and fixtures
- `mysql/`, `postgresql/`: import scripts
- `alternatives/dotnet/`: experimental C# parser/exporter
- `tmp/`: typical location for downloaded dump files (do not commit large dumps)

## Setup
- Python 3.6+ is required.
- Install runtime deps: `pip install -r requirements.txt`.
- For tests and linting: `pip install -r requirements-dev.txt`.

## Common commands
- Show CLI usage: `python run.py --help`
- Run exporter: `python run.py [--bz2] [--apicounts] [--export <entity>...] --output <dir> <dump-dir | dump-files...>`
- Run tests: `python -m pytest`

## Conventions
- Keep changes minimal and avoid drive-by reformatting.
- Respect the 120-character line limit from `setup.cfg`.
- Prefer existing patterns in the codebase (e.g., argument handling in `run.py` and `discogsxml2db/exporter.py`).
- Be cautious with performance-sensitive paths; these dumps are large in real usage.

## When touching .NET code
- Keep changes scoped to `alternatives/dotnet` and follow that project’s README.

