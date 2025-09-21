# -*- coding: utf-8 -*-
"""Command-line entry point for the speedup CSV exporter."""
import bz2
import csv
import glob
import os
import argparse

from tqdm import tqdm


from .parser import *

from xml_utils import DumpEntity, ensure_root_wrapper


def limited_getattr(entity, i, default="", limit=8192):
    v = getattr(entity, i, default)

    if type(v) != str:
        return v

    if limit < len(v):
        print(f"Long attr val {entity}, {i}, {len(v)}")
        return v[:limit]
    else:
        return v


def _write_entity(writer, entity, fields):
    row = [limited_getattr(entity, i, "") for i in fields]
    writer.writerow(row)


def _write_fields_rows(writer, entity, name, fields):
    rows = [
        [entity.id] + [limited_getattr(element, i, "") for i in fields]
        for element in getattr(entity, name, [])
    ]

    writer.writerows(rows)


def _write_rows(writer, entity, name):
    rows = [
        [entity.id, element] for element in limited_getattr(entity, name, []) if element
    ]

    writer.writerows(rows)


_parsers = {
    "artist": DiscogsArtistParser,
    "label": DiscogsLabelParser,
    "master": DiscogsMasterParser,
    "release": DiscogsReleaseParser,
}


class EntityCsvExporter(object):
    """Read a Discogs dump XML file and exports SQL table records as CSV."""

    def __init__(
        self,
        entity,
        idir,
        odir,
        limit=None,
        bz2=True,
        dry_run=False,
        debug=False,
        max_hint=None,
        verbose=False,
        show_progress=True,
    ):
        self.entity = entity
        self.parser = _parsers[entity]()
        self.max_hint = max_hint
        self.verbose = verbose
        self.show_progress = show_progress

        lookup = "discogs_[0-9]*_{}s.xml*".format(entity)
        self.pattern = os.path.join(idir, lookup)

        # where and how the exporter will write to
        self.odir = odir
        self.limit = limit
        self.bz2 = bz2
        self.dry_run = dry_run
        self.write_csv_headers = True

        self.debug = debug
        self.progress_bar_width = 120

    def openfile(self):
        matches = sorted(glob.glob(self.pattern))
        if not matches:
            raise RuntimeError("No dump files found for pattern %s" % self.pattern)

        def priority(path):
            if path.endswith(".xml.gz"):
                return 0
            if path.endswith(".xml"):
                return 1
            return 2

        matches.sort(key=priority)
        return matches[0]

    def export(self):
        return self.export_from_file(self.openfile())

    @staticmethod
    def validate(entity):
        return True

    def build_ops(self):
        if self.bz2:
            openf = bz2.open
            ftemplate = "{table}.csv.bz2"
        else:
            openf = open
            ftemplate = "{table}.csv"

        operations = []
        for table, func, args in self.actions:
            fname = ftemplate.format(table=table)
            outfp = openf(os.path.join(self.odir, fname), "wt", encoding="utf-8")
            writer = csv.writer(outfp)

            if self.write_csv_headers:
                writer.writerow(csv_headers[table])

            operations.append((writer, func, args, outfp))
        return operations

    def run_ops(self, entity, operations):
        for writer, f, args, _ in operations:
            if args is not None:
                f(writer, entity, *args)
            else:
                f(writer, entity)

    def clean_ops(self, operations):
        for _, _, _, fp in operations:
            fp.close()

    def export_from_file(self, source):
        entity = DumpEntity.from_key(self.entity)
        if not isinstance(source, (str, os.PathLike)):
            raise TypeError("export_from_file expected a filesystem path")

        with ensure_root_wrapper(source, entity) as wrapped:
            return self._export_stream(wrapped.stream)

    def _export_stream(self, fp):
        if not self.dry_run:
            operations = self.build_ops()

        iterator = enumerate(filter(self.validate, self.parser.parse(fp)), start=1)
        cnt = 0

        if self.show_progress:
            with tqdm(
                total=self.max_hint,
                ncols=self.progress_bar_width,
                desc="Processing {:>10}s".format(self.entity),
                unit="{}s".format(self.entity),
            ) as pbar:

                for cnt, entity in iterator:
                    if not self.dry_run:
                        self.run_ops(entity, operations)
                    pbar.update()
                    if self.limit is not None and cnt >= self.limit:
                        break
        else:
            for cnt, entity in iterator:
                if not self.dry_run:
                    self.run_ops(entity, operations)
                if self.limit is not None and cnt >= self.limit:
                    break

        if not self.dry_run:
            self.clean_ops(operations)
        return cnt


class LabelExporter(EntityCsvExporter):

    def __init__(self, *args, **kwargs):
        super().__init__("label", *args, **kwargs)

        main_fields = [
            "id",
            "name",
            "contactinfo",
            "profile",
            "parentLabel",
            "data_quality",
        ]
        image_fields = ["type", "width", "height"]
        self.actions = (
            ("label", _write_entity, [main_fields]),
            ("label_url", _write_rows, ["urls"]),
            ("label_image", _write_fields_rows, ["images", image_fields]),
        )

    def validate(self, label):
        if not label.name:
            return False
        return True


class ArtistExporter(EntityCsvExporter):

    def __init__(self, *args, **kwargs):
        super().__init__("artist", *args, **kwargs)

        main_fields = ["id", "name", "realname", "profile", "data_quality"]
        image_fields = ["type", "width", "height"]
        self.actions = (
            ("artist", _write_entity, [main_fields]),
            ("artist_alias", _write_rows, ["aliases"]),
            ("artist_namevariation", _write_rows, ["namevariations"]),
            ("artist_url", _write_rows, ["urls"]),
            ("artist_image", _write_fields_rows, ["images", image_fields]),
            ("group_member", self.write_group_members, None),
        )

    @staticmethod
    def write_group_members(writer, artist):
        writer.writerows(
            [
                [artist.id, member_id, member_name]
                for member_id, member_name in getattr(artist, "members", [])
            ]
        )

    def validate(self, artist):
        if not artist.name:
            artist.name = "[artist #%d]" % artist.id
        return True


class MasterExporter(EntityCsvExporter):

    def __init__(self, *args, **kwargs):
        super().__init__("master", *args, **kwargs)

        main_fields = ["id", "title", "year", "main_release", "data_quality"]
        artist_fields = ["id", "name", "anv", "position", "join", "role"]
        video_fields = ["duration", "title", "description", "src"]
        image_fields = ["type", "width", "height"]
        self.actions = (
            ("master", _write_entity, [main_fields]),
            ("master_artist", _write_fields_rows, ["artists", artist_fields]),
            ("master_video", _write_fields_rows, ["videos", video_fields]),
            ("master_genre", _write_rows, ["genres"]),
            ("master_style", _write_rows, ["styles"]),
            ("master_image", _write_fields_rows, ["images", image_fields]),
        )


class ReleaseExporter(EntityCsvExporter):

    def __init__(self, *args, **kwargs):
        super().__init__("release", *args, **kwargs)

        main_fields = [
            "id",
            "title",
            "released",
            "country",
            "notes",
            "data_quality",
            "master_id",
            "status",
        ]
        label_fields = ["name", "catno"]
        video_fields = ["duration", "title", "description", "src"]
        format_fields = ["name", "qty", "text", "descriptions"]
        company_fields = [
            "id",
            "name",
            "entity_type",
            "entity_type_name",
            "resource_url",
        ]
        identifier_fields = ["description", "type", "value"]
        track_fields = [
            "sequence",
            "position",
            "parent",
            "title",
            "duration",
            "track_id",
        ]
        image_fields = ["type", "width", "height"]

        self.artist_fields = [
            "id",
            "name",
            "extra",
            "anv",
            "position",
            "join",
            "role",
            "tracks",
        ]

        self.actions = (
            ("release", _write_entity, [main_fields]),
            ("release_genre", _write_rows, ["genres"]),
            ("release_style", _write_rows, ["styles"]),
            ("release_label", _write_fields_rows, ["labels", label_fields]),
            ("release_video", _write_fields_rows, ["videos", video_fields]),
            ("release_format", _write_fields_rows, ["formats", format_fields]),
            ("release_company", _write_fields_rows, ["companies", company_fields]),
            (
                "release_identifier",
                _write_fields_rows,
                ["identifiers", identifier_fields],
            ),
            ("release_track", _write_fields_rows, ["tracklist", track_fields]),
            # Two special operations
            ("release_artist", self.write_artists, None),
            ("release_track_artist", self.write_track_artists, None),
            ("release_image", _write_fields_rows, ["images", image_fields]),
        )

    def write_artists(self, writer, release):
        _write_fields_rows(writer, release, "artists", self.artist_fields)
        _write_fields_rows(writer, release, "extraartists", self.artist_fields)

    def write_track_artists(self, writer, release):
        writer.writerows(
            (
                [release.id, track.sequence, track.track_id]
                + [getattr(element, i, "") for i in self.artist_fields]
            )
            for track in getattr(release, "tracklist", [])
            for element in (
                getattr(track, "artists", []) + getattr(track, "extraartists", [])
            )
        )


_exporters = {
    "label": LabelExporter,
    "artist": ArtistExporter,
    "master": MasterExporter,
    "release": ReleaseExporter,
}


DEFAULT_ROUGH_COUNTS = {
    "artists": 5000000,
    "labels": 1100000,
    "masters": 1250000,
    "releases": 8500000,
}


DEFAULT_EXPORT_ENTITIES = tuple(_exporters.keys())


csv_headers = {
    table: columns.split()
    for table, columns in {
        "label": "id name contact_info profile parent_name data_quality",
        "label_url": "label_id url",
        "label_image": "label_id type width height",
        "artist": "id name realname profile data_quality",
        "artist_alias": "artist_id alias_name",
        "artist_namevariation": "artist_id name",
        "artist_url": "artist_id url",
        "group_member": "group_artist_id member_artist_id member_name",
        "artist_image": "artist_id type width height",
        "master": "id title year main_release data_quality",
        "master_artist": "master_id artist_id artist_name anv position join_string role",
        "master_video": "master_id duration title description uri",
        "master_genre": "master_id genre",
        "master_style": "master_id style",
        "master_image": "master_id type width height",
        "release": "id title released country notes data_quality master_id status",
        "release_artist": "release_id artist_id artist_name extra anv position join_string role tracks",
        "release_label": "release_id label_name catno",
        "release_genre": "release_id genre",
        "release_style": "release_id style",
        "release_format": "release_id name qty text_string descriptions",
        "release_company": "release_id company_id company_name entity_type entity_type_name uri",
        "release_video": "release_id duration title description uri",
        "release_identifier": "release_id description type value",
        "release_track": "release_id sequence position parent title duration track_id",
        "release_track_artist": "release_id track_sequence track_id artist_id artist_name extra anv position join_string role tracks",
        "release_image": "release_id type width height",
    }.items()
}


def _fetch_rough_counts(use_api_counts=False, request_session=None):
    counts = DEFAULT_ROUGH_COUNTS.copy()
    if not use_api_counts:
        return counts

    if request_session is not None:
        session = request_session
    else:
        try:
            import requests as requests_module
        except ImportError:
            return counts
        session = requests_module

    try:
        response = session.get("https://api.discogs.com/", timeout=5)
        payload = response.json()
        statistics = payload.get("statistics", {}) if isinstance(payload, dict) else {}
        for key, value in statistics.items():
            if isinstance(value, int):
                counts[key] = value
    except Exception:
        # Swallow any network/JSON issues and fall back to built-in guesses.
        pass

    return counts


def export_entities(
    input_dir,
    output_dir,
    entities,
    *,
    limit=None,
    bz2_on=False,
    debug=False,
    dry_run=False,
    use_api_counts=False,
    show_progress=True,
    request_session=None,
):
    if not entities:
        entities = DEFAULT_EXPORT_ENTITIES

    counts = _fetch_rough_counts(use_api_counts=use_api_counts, request_session=request_session)

    for entity in entities:
        exporter_cls = _exporters[entity]
        expected_key = f"{entity}s"
        expected_count = counts.get(expected_key)

        max_hint = None
        if show_progress:
            max_hint = expected_count
            if limit is not None and expected_count is not None:
                max_hint = min(expected_count, limit)

        exporter = exporter_cls(
            input_dir,
            output_dir,
            limit=limit,
            bz2=bz2_on,
            debug=debug,
            max_hint=max_hint,
            dry_run=dry_run,
            show_progress=show_progress,
        )
        exporter.export()


def main(argv=None):
    parser = argparse.ArgumentParser(
        prog="discogs-speedup-exporter",
        description="Export Discogs XML dumps to relational CSV tables.",
    )
    parser.add_argument(
        "input_dir",
        help="Directory containing Discogs XML dumps.",
    )
    parser.add_argument(
        "output_dir",
        nargs="?",
        default=".",
        help="Directory where CSV files will be written (default: current directory).",
    )
    parser.add_argument(
        "--bz2",
        dest="bz2_on",
        action="store_true",
        help="Compress generated CSV files using bz2.",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=None,
        help="Limit export to at most this many entities per file.",
    )
    parser.add_argument(
        "--export",
        dest="entities",
        action="append",
        choices=DEFAULT_EXPORT_ENTITIES,
        help="Restrict export to the selected entity (repeatable).",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable verbose debugging output from the exporter.",
    )
    parser.add_argument(
        "--apicounts",
        action="store_true",
        help="Fetch expected entity counts from the Discogs API to improve progress bars.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Parse the dumps without writing any files.",
    )
    parser.add_argument(
        "--no-progress",
        dest="show_progress",
        action="store_false",
        help="Disable the progress bar output.",
    )
    parser.set_defaults(show_progress=True)

    args = parser.parse_args(argv)

    export_entities(
        args.input_dir,
        args.output_dir,
        entities=args.entities or DEFAULT_EXPORT_ENTITIES,
        limit=args.limit,
        bz2_on=args.bz2_on,
        debug=args.debug,
        dry_run=args.dry_run,
        use_api_counts=args.apicounts,
        show_progress=args.show_progress,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
