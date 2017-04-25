# -*- coding: utf-8 -*-
import gzip
from io import BytesIO
import os
import xml.sax

from discogsartistparser import ArtistHandler
from discogslabelparser import LabelHandler
from discogsmasterparser import MasterHandler
from discogsreleaseparser import ReleaseHandler

from tests import samplesdir


class DummyExporter(object):

    def storeArtist(self, artist):
        self.dump(artist)

    def storeLabel(self, label):
        self.dump(label)

    def storeRelease(self, release):
        self.dump(release)

    def storeMaster(self, master):
        self.dump(master)

    def dump(self, thing):
        pass

    def finish(self, completely_done=False):
        pass


class CountingExporter(DummyExporter):

    def __init__(self):
        self.counts = {
            'Artist': 0,
            'Label': 0,
            'Master': 0,
            'Release': 0,
        }

    def dump(self, thing):
        self.counts[thing.__class__.__name__] += 1


class AppendingExporter(DummyExporter):

    def __init__(self):
        self.store = {
            'Artist': [],
            'Label': [],
            'Master': [],
            'Release': [],
        }

    def dump(self, thing):
        self.store[thing.__class__.__name__].append(thing)


class TestArtistParser(object):

    def test_basic_unicode(self):
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ArtistHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(b'''<artists>
  <artist>
    <images>
      <image height="450" type="secondary" uri="" uri150="" width="600"/>
    </images>
    <id>4678</id>
    <name>The Persuader</name>
    <realname>Jesper Dahlb&#xE4;ck</realname>
    <profile>Capricorn born 1972, homebase Stockholm, Sweden.
    Performing Vinyl DJ sets, creating Audio / Music productions, and graphic works.</profile>
    <data_quality>Correct</data_quality>
    <urls>
      <url>http://www.selwaymusic.net</url>
      <url>http://www.facebook.com/pages/John-Selway/224601153031?ref=ts&amp;fref=ts</url>
      <url>http://www.myspace.com/selwaymusic</url>
    </urls>
    <namevariations>
      <name>Persuader</name>
      <name>The Presuader</name>
    </namevariations>
    <aliases>
      <name>Dick Track</name>
      <name>Faxid</name>
      <name>Groove Machine</name>
      <name>Janne Me' Amazonen</name>
      <name>Jesper Dahlb&#xE4;ck</name>
      <name>Lenk</name>
      <name>The Pinguin Man</name>
    </aliases>
    <groups>
      <name>Christian Smith &amp; John Selway</name>
      <name>CSM</name>
      <name>Dharma (2)</name>
      <name>Disintegrator</name>
      <name>East Side Scientific</name>
      <name>Koenig Cylinders</name>
      <name>Machines (8)</name>
      <name>Moods</name>
      <name>Neurotic Drum Band</name>
      <name>Octaves/Tremelos</name>
      <name>Prana (2)</name>
      <name>Psychedelic Research Lab</name>
      <name>Rancho Relaxo Allstars</name>
      <name>Responsible Space Playboys</name>
      <name>Synapse</name>
      <name>The Founders</name>
    </groups>
  </artist>
</artists>'''))
        assert len(exporter.store['Artist']) == 1, exporter.store
        artist = exporter.store['Artist'][0]

        assert artist.id == 4678, exporter.store
        assert artist.name == 'The Persuader', exporter.store
        assert artist.realname == u'Jesper Dahlbäck', exporter.store

        assert len(artist.aliases) == 7, exporter.store
        assert artist.aliases[0] == u'Dick Track', exporter.store

        assert len(artist.namevariations) == 2, exporter.store
        assert artist.namevariations[0] == u'Persuader', exporter.store

        assert len(artist.groups) == 16, exporter.store
        assert artist.groups[0] == u'Christian Smith & John Selway', exporter.store

        assert len(artist.urls) == 3, exporter.store
        assert artist.urls[0] == u'http://www.selwaymusic.net', exporter.store

        assert len(artist.images) == 1, exporter.store
        assert len(artist.profile) > 0, exporter.store


class TestGzipFileParser(object):

    def test_artists_xml(self):

        parser = xml.sax.make_parser()
        exporter = CountingExporter()
        entityHandler = ArtistHandler(exporter)
        parser.setContentHandler(entityHandler)
        fname = os.path.join(samplesdir,
                             '20170401/head50/discogs_20170401_artists.xml.gz')
        parser.parse(gzip.open(fname))
        assert exporter.counts['Artist'] == 27, exporter.counts

    def test_labels_xml(self):

        parser = xml.sax.make_parser()
        exporter = CountingExporter()
        entityHandler = LabelHandler(exporter)
        parser.setContentHandler(entityHandler)
        fname = os.path.join(samplesdir,
                             '20170401/head50/discogs_20170401_labels.xml.gz')
        parser.parse(gzip.open(fname))
        assert exporter.counts['Label'] == 5, exporter.counts

    def test_masters_xml(self):

        parser = xml.sax.make_parser()
        exporter = CountingExporter()
        entityHandler = MasterHandler(exporter)
        parser.setContentHandler(entityHandler)
        fname = os.path.join(samplesdir,
                             '20170401/head50/discogs_20170401_masters.xml.gz')
        parser.parse(gzip.open(fname))
        assert exporter.counts['Master'] == 47, exporter.counts

    def test_releases_xml(self):

        parser = xml.sax.make_parser()
        exporter = CountingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        fname = os.path.join(samplesdir,
                             '20170401/head50/discogs_20170401_releases.xml.gz')
        parser.parse(gzip.open(fname))
        assert exporter.counts['Release'] == 10, exporter.counts
