import logging
import os
import pathlib
import shutil
import tempfile
import csv

from typing import Dict

try:
    from discogsxml2db.exporter import main as export_main
except ImportError:
    import sys

    parent_path = str(pathlib.Path(__file__).absolute().parent.parent)
    sys.path.insert(1, parent_path)
    from discogsxml2db.exporter import main as export_main


class TestExtraction:
    _samples_folder: str = None
    _resulting_counts: Dict[str, Dict[str, int]] = {
        "label": {"label.csv": 1000, "label_image.csv": 421, "label_url.csv": 437,},
        "artist": {
            "artist_alias.csv": 548,
            "artist.csv": 1000,
            "artist_image.csv": 530,
            "artist_namevariation.csv": 893,
            "artist_url.csv": 460,
            "group_member.csv": 218,
        },
        "master": {
            "master_artist.csv": 1174,
            "master_genre.csv": 1315,
            "master_image.csv": 3020,
            "master_style.csv": 1705,
            "master.csv": 1000,
            "master_video.csv": 2176,
        },
        "release": {
            "release_artist.csv": 5053,
            "release_company.csv": 1678,
            "release_format.csv": 1037,
            "release_genre.csv": 1302,
            "release_identifier.csv": 1672,
            "release_image.csv": 2897,
            "release_label.csv": 1135,
            "release_style.csv": 1496,
            "release_track_artist.csv": 6721,
            "release_track.csv": 9430,
            "release_video.csv": 2324,
            "release.csv": 1000,
        },
    }

    @classmethod
    def setup_class(cls):
        cls._samples_folder = os.path.join(pathlib.Path(__file__).absolute().parent, "samples")

    def test_artists_counts(self, tmp_path):
        self._check_counts("artist", tmp_path)

    def test_labels_counts(self, tmp_path):
        self._check_counts("label", tmp_path)

    def test_masters_counts(self, tmp_path):
        self._check_counts("master", tmp_path)

    def test_releases_counts(self, tmp_path):
        self._check_counts("release", tmp_path)

    def _check_counts(self, entity, tmp_path):
        # run exporter with arguments:
        # - INPUT=path to samples
        # - OUTPUT=path to temp folder
        # - export=label

        arguments = {
            "INPUT": self._samples_folder,
            "OUTPUT": tmp_path,
            "--export": [entity],
            "--limit": None,
            "--bz2": False,
            "--debug": True,
            "--dry-run": False,
            "--apicounts": False,
        }

        logging.debug("Counting %s: %r", entity, arguments)

        # act
        export_main(arguments)

        # asserts
        for file_name in self._resulting_counts[entity]:
            csv_file = os.path.join(tmp_path, file_name)
            logging.info("Testing for file %s", csv_file)
            assert(os.path.exists(csv_file), f"Expected {csv_file} to exist.")
            actual_records = self._count_records(csv_file)
            expected_records = self._resulting_counts[entity][file_name]
            assert expected_records == actual_records

    @staticmethod
    def _count_records(csv_file_path: str) -> int:
        with open(csv_file_path, newline="") as csv_file:
            csv_reader = csv.reader(csv_file)
            row_count = sum(1 for row in csv_reader)
        return row_count - 1  # first is header
