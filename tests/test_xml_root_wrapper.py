# -*- coding: utf-8 -*-
import xml.sax

import pytest

from parsers.discogsartistparser import ArtistHandler
from speedup.parser import DiscogsArtistParser
from xml_utils import DumpEntity, RootValidationError, ensure_root_wrapper


def write_xml(tmp_path, name, content):
    path = tmp_path / name
    path.write_text(content, encoding="utf-8")
    return path


def test_wrapper_adds_missing_root_boundaries(tmp_path):
    path = write_xml(
        tmp_path,
        "discogs_20240101_artists.xml",
        "<artist><id>1</id><name>Test</name></artist>",
    )

    with ensure_root_wrapper(path, DumpEntity.ARTIST) as wrapped:
        payload = wrapped.stream.read()
        missing_open = wrapped.missing_open
        missing_close = wrapped.missing_close

    assert missing_open and missing_close
    assert payload.startswith(b"<artists>")
    assert payload.rstrip().endswith(b"</artists>")


def test_wrapper_leaves_well_formed_root_untouched(tmp_path):
    content = (
        "<artists>\n"
        "  <artist><id>1</id><name>Alpha</name></artist>\n"
        "  <artist><id>2</id><name>Beta</name></artist>\n"
        "</artists>\n"
    )
    path = write_xml(tmp_path, "discogs_20240101_artists.xml", content)

    with ensure_root_wrapper(path, DumpEntity.ARTIST) as wrapped:
        payload = wrapped.stream.read()
        patched = wrapped.patched

    assert not patched
    assert payload == content.encode("utf-8")


class RecordingExporter(object):

    def __init__(self):
        self.artists = []

    def storeArtist(self, artist):
        self.artists.append(artist)

    def finish(self, completely_done=False):
        pass


def test_wrapper_works_with_legacy_sax_parser(tmp_path):
    xml_payload = (
        "<artists>\n"
        "  <artist>\n"
        "    <id>9</id>\n"
        "    <name>Legacy Artist</name>\n"
        "  </artist>\n"
        ""
    )
    path = write_xml(tmp_path, "discogs_20240101_artists.xml", xml_payload)

    exporter = RecordingExporter()
    handler = ArtistHandler(exporter)
    parser = xml.sax.make_parser()
    parser.setContentHandler(handler)

    with ensure_root_wrapper(path, DumpEntity.ARTIST) as wrapped:
        parser.parse(wrapped.stream)

    assert len(exporter.artists) == 1
    assert exporter.artists[0].name == "Legacy Artist"


def test_wrapper_works_with_speedup_parser(tmp_path):
    xml_payload = (
        "<artist>\n"
        "  <id>17</id>\n"
        "  <name>Speed Artist</name>\n"
        "</artist>\n"
    )
    path = write_xml(tmp_path, "discogs_20240101_artists.xml", xml_payload)

    parser = DiscogsArtistParser()
    with ensure_root_wrapper(path, DumpEntity.ARTIST) as wrapped:
        artists = list(parser.parse(wrapped.stream))

    assert len(artists) == 1
    assert artists[0].name == "Speed Artist"


def test_wrapper_can_fail_in_strict_mode(tmp_path, monkeypatch):
    path = write_xml(
        tmp_path,
        "discogs_20240101_artists.xml",
        "<artist><id>1</id><name>Strict Mode</name></artist>",
    )
    monkeypatch.setenv("DISCOGS_XML_STRICT_ROOT", "1")

    with pytest.raises(RootValidationError):
        with ensure_root_wrapper(path, DumpEntity.ARTIST):
            pass
