# Plan: Click CLI for `speedup/exporter`

## Goals
- Provide a user-friendly `uv run python -m speedup.cli` (and optional console script) that wraps the existing CSV exporter.
- Preserve current functionality (`--bz2`, `--dry-run`, entity selection, etc.) while adding structured help, validation, and progress feedback.
- Keep backwards compatibility by letting `speedup/exporter.py` stay scriptable during the transition.

## Key Design Decisions
# Update: top-level CLI should be called `discogs-export`.
1. **Command layout**: implement a single `export` command under a Click group (`discogs-export`). Group leaves room for future subcommands (e.g., checksum, validate, summarize).
2. **Parameter modelling**: replicate current docopt options as Click options with clear types and defaults. Use enums for entities and `MultipleChoiceOption` style for repeated `--export`.
3. **Configuration mapping**: create a thin adapter (`speedup/cli.py`) that translates Click parameters into the `EntityCsvExporter` workflow, reusing helper functions where possible.
4. **Progress reporting**: continue to rely on `tqdm`; expose `--no-progress` toggle if necessary for non-interactive environments.
5. **Error handling**: unify runtime exceptions into human-readable messages (e.g., missing dump directory) using Click's exception helpers (`BadParameter`, etc.).
6. **Testing strategy**: add unit tests with Click's CliRunner plus a smoke test exercising dry-run parsing against fixture XML in `tests/samples/`.

## Implementation Steps
1. **Refactor exporter entry points**
   - Move top-level `if __name__ == '__main__'` logic from `speedup/exporter.py` into a callable (e.g., `run_export`) that accepts args already parsed.
   - Ensure existing script entry still works by calling the new function with `docopt`-parsed args temporarily.
2. **Introduce Click CLI module**
   - Add `speedup/cli.py` defining a Click group (`discogs-export`) and `export` command with options mirroring the docopt API (`--bz2/--no-bz2`, `--limit`, `--export`, `--debug`, `--apicounts`, `--dry-run`).
   - Support multiple `INPUT` directories by validating paths, defaulting `OUTPUT` to current behaviour when omitted.
   - Plan an additional `files` subcommand that accepts multiple XML/XML.GZ dump files directly (bypassing directory discovery) and streams them through the same exporter pipeline.
3. **Wire the CLI to the exporter**
   - Instantiate `EntityCsvExporter` (or multiple instances for selected entities) based on Click options.
   - Handle `--export` absence by iterating over all supported entities.
   - Surface exceptions with `click.ClickException`.
4. **Package integration**
   - Update `pyproject.toml` to register a console script (`discogs-export = speedup.cli:main`).
   - Add Click as a dependency with `uv add click` before wiring the console script.
   - Add documentation references in `EXPORTERS.md` and README once the CLI ships.
5. **Testing & validation**
   - Write tests under `tests/test_speedup_cli.py` using `CliRunner` to cover: help text, option parsing (bz2 toggle, repeated `--export`), and dry-run behaviour against sample data.
   - Run `uv run python -m pytest` to ensure coverage.
6. **Robust XML sanitation**
   - Reproduce the "Error: PCDATA invalid Char value 3" failure by feeding the offending dump snippet to the SAX pipeline.
   - Add a preprocessing stage (e.g., incremental replacement of control characters or `xmlcharrefreplace` sanitiser) that strips invalid code points before handing the stream to `lxml`.
   - Guard both directory and `files` subcommands with the sanitizer and surface a friendly Click error suggesting the fix if the data still fails to parse.
7. **Deprecation messaging**
   - Optionally add a warning in `speedup/exporter.py` suggesting the Click CLI for new usage, keeping docopt interface until a major release removes it.
8. **Fixup subcommand** Implement a subcommand to cleanup legacy xml
   dump files that might have not have an appropriate root element and
   Emojibake. Use the Python ftfy module to handle this.

## IMPLEMENTATION
- Replaced `docopt` in `speedup/exporter.py` with `argparse`, exposing `export_entities` and optional progress control so the scriptable entry point and Click CLI share logic.
- Added `speedup/cli.py` with a `discogs-export` Click group/command that mirrors all legacy flags and dispatches to `export_entities`.
- Updated `pyproject.toml`/`uv.lock` to include `click` and `tqdm`, and registered the `discogs-export = speedup.cli:main` console script.
- Introduced `tests/test_speedup_cli.py` using `CliRunner` plus direct `export_entities` validation against the bundled sample dumps.
- Refreshed docs (`EXPORTERS.md`, README) to advertise the new CLI alongside the legacy invocation.
