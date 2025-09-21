import os
from pathlib import Path

from click.testing import CliRunner

from speedup import exporter
from speedup.cli import cli


SAMPLES_DIR = Path(__file__).parent / "samples" / "20170401" / "head50"


def test_discogs_export_creates_csvs(tmp_path):
    output_dir = tmp_path / "csv"
    runner = CliRunner()
    result = runner.invoke(
        cli,
        [
            "export",
            str(SAMPLES_DIR),
            str(output_dir),
            "--limit",
            "2",
            "--export",
            "release",
            "--no-progress",
        ],
        catch_exceptions=False,
    )

    assert result.exit_code == 0, result.output

    release_csv = output_dir / "release.csv"
    assert release_csv.exists()
    rows = release_csv.read_text(encoding="utf-8").strip().splitlines()
    # Header plus at least one data row when limit >= 1
    assert len(rows) >= 2
    assert rows[0].startswith("id,title,released")


def test_discogs_export_dry_run_leaves_no_artifacts(tmp_path):
    output_dir = tmp_path / "dry"
    runner = CliRunner()
    result = runner.invoke(
        cli,
        [
            "export",
            str(SAMPLES_DIR),
            str(output_dir),
            "--export",
            "artist",
            "--limit",
            "1",
            "--dry-run",
            "--no-progress",
        ],
        catch_exceptions=False,
    )

    assert result.exit_code == 0, result.output
    # Directory is created but should remain empty when dry-run is enabled
    assert list(output_dir.iterdir()) == []


def test_export_entities_uses_default_entities_when_none(tmp_path):
    output_dir = tmp_path / "defaults"
    os.makedirs(output_dir, exist_ok=True)

    exporter.export_entities(
        str(SAMPLES_DIR),
        str(output_dir),
        entities=(),
        limit=1,
        show_progress=False,
    )

    # A default table (artists) should be generated.
    assert (output_dir / "artist.csv").exists()
