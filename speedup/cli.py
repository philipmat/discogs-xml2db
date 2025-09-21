"""Click-based command line interface for the speedup exporters."""
import os

import click

from .exporter import DEFAULT_EXPORT_ENTITIES, export_entities


@click.group(help="Utilities for exporting Discogs XML dumps with the speedup pipeline.")
def cli():
    """Root command group for discogs-export."""
    pass


@cli.command("export", help="Convert Discogs XML dumps into CSV/CSV.BZ2 tables.")
@click.argument(
    "input_dir",
    type=click.Path(exists=True, file_okay=False, readable=True, path_type=str),
)
@click.argument(
    "output_dir",
    required=False,
    default=".",
    type=click.Path(file_okay=False, writable=True, path_type=str),
)
@click.option(
    "--bz2/--no-bz2",
    "bz2_on",
    default=False,
    show_default=True,
    help="Compress generated CSV files using bz2.",
)
@click.option(
    "--limit",
    type=click.IntRange(min=1),
    default=None,
    help="Limit export to a maximum number of entities per file.",
)
@click.option(
    "--export",
    "entities",
    type=click.Choice(DEFAULT_EXPORT_ENTITIES),
    multiple=True,
    help="Restrict the export to the selected entity type(s). Repeatable.",
)
@click.option(
    "--debug/--no-debug",
    default=False,
    show_default=True,
    help="Enable verbose debugging output from the exporter.",
)
@click.option(
    "--apicounts/--no-apicounts",
    default=False,
    show_default=True,
    help="Fetch expected entity counts from the Discogs API to improve progress bars.",
)
@click.option(
    "--dry-run/--no-dry-run",
    default=False,
    show_default=True,
    help="Parse the dump without writing any output files.",
)
@click.option(
    "--progress/--no-progress",
    "show_progress",
    default=True,
    show_default=True,
    help="Display a tqdm progress bar during export.",
)
def export_command(
    input_dir,
    output_dir,
    bz2_on,
    limit,
    entities,
    debug,
    apicounts,
    dry_run,
    show_progress,
):
    """Export Discogs XML dumps to CSV tables using the speedup pipeline."""
    selected_entities = tuple(entities) if entities else DEFAULT_EXPORT_ENTITIES

    try:
        os.makedirs(output_dir, exist_ok=True)
    except OSError as exc:
        raise click.ClickException(
            f"Unable to create output directory '{output_dir}': {exc}"
        ) from exc

    try:
        export_entities(
            input_dir,
            output_dir,
            selected_entities,
            limit=limit,
            bz2_on=bz2_on,
            debug=debug,
            dry_run=dry_run,
            use_api_counts=apicounts,
            show_progress=show_progress,
        )
    except Exception as exc:  # pragma: no cover - re-raised for debug convenience
        if debug:
            raise
        raise click.ClickException(str(exc)) from exc


def main():
    """Entrypoint used by console scripts."""
    cli()


if __name__ == "__main__":
    main()
