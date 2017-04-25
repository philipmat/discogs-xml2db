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
        assert artist.name == u'The Persuader', exporter.store
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


class TestLabelParser(object):

    def test_basic_unicode(self):
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = LabelHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(b'''<labels>
  <label>
    <images>
      <image height="24" type="primary" uri="" uri150="" width="132"/>
      <image height="126" type="secondary" uri="" uri150="" width="587"/>
      <image height="196" type="secondary" uri="" uri150="" width="600"/>
      <image height="121" type="secondary" uri="" uri150="" width="275"/>
      <image height="720" type="secondary" uri="" uri150="" width="382"/>
      <image height="398" type="secondary" uri="" uri150="" width="500"/>
      <image height="189" type="secondary" uri="" uri150="" width="600"/>
    </images>
    <id>42</id>
    <name>Planet E</name>
    <contactinfo>Planet E Communications&#xD;
P.O. Box 27218&#xD;
Detroit, Michigan, MI 48227&#xD;
USA&#xD;
&#xD;
Phone: +1 313 874 8729&#xD;
Fax: +1 313 874 8732&#xD;
Email: info@Planet-e.net</contactinfo>
    <profile>[a=Carl Craig]'s classic techno label founded in 1991.&#xD;
&#xD;
On at least 1 release, Planet E is listed as publisher.</profile>
    <data_quality>Correct</data_quality>
    <urls>
      <url>http://planet-e.net/</url>
      <url>http://planetecommunications.bandcamp.com/</url>
      <url>http://www.discogs.com/user/planetedetroit</url>
      <url>https://www.facebook.com/planetedetroit</url>
      <url>https://www.flickr.com/photos/planetedetroit</url>
      <url>https://plus.google.com/100841702106447505236</url>
      <url>https://myspace.com/planetecom</url>
      <url>https://myspace.com/planetedetroit</url>
      <url>https://soundcloud.com/planetedetroit</url>
      <url>https://twitter.com/planetedetroit</url>
      <url>https://vimeo.com/user1265384</url>
      <url>https://www.youtube.com/user/planetedetroit</url>
      <url>https://en.wikipedia.org/wiki/Planet_E_Communications</url>
    </urls>
    <sublabels>
      <label>Antidote (4)</label>
      <label>Community Projects</label>
      <label>Guilty Pleasures</label>
      <label>I Ner Zon Sounds</label>
      <label>Planet E Communications</label>
      <label>Planet E Communications, Inc.</label>
      <label>TWPENTY</label>
    </sublabels>
  </label>
</labels>'''))
        assert len(exporter.store['Label']) == 1, exporter.store
        label = exporter.store['Label'][0]

        assert label.id == 42, exporter.store
        assert label.name == u'Planet E', exporter.store

        assert len(label.sublabels) == 7, exporter.store
        assert label.sublabels[0] == u'Antidote (4)', exporter.store

        assert len(label.urls) == 13, exporter.store
        assert label.urls[0] == u'http://planet-e.net/', exporter.store

        assert len(label.profile) > 0, exporter.store
        assert len(label.contactinfo) > 0, exporter.store


class TestMasterParser(object):

    def test_basic_unicode(self):
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = MasterHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(b'''<masters>
  <master id="18500">
    <main_release>155102</main_release>
    <images>
      <image height="588" type="primary" uri="" uri150="" width="600"/>
    </images>
    <artists>
      <artist>
        <id>308</id>
        <name>Ed Rush</name>
        <anv/>
        <join>-</join>
        <role/>
        <tracks/>
      </artist>
      <artist>
        <id>2823</id>
        <name>DJ Trace</name>
        <anv>Trace</anv>
        <join>-</join>
        <role/>
        <tracks/>
      </artist>
      <artist>
        <id>285</id>
        <name>Fierce</name>
        <anv/>
        <join>-</join>
        <role/>
        <tracks/>
      </artist>
      <artist>
        <id>168579</id>
        <name>Nico (4)</name>
        <anv/>
        <join>-</join>
        <role/>
        <tracks/>
      </artist>
      <artist>
        <id>218</id>
        <name>Optical</name>
        <anv/>
        <join/>
        <role/>
        <tracks/>
      </artist>
    </artists>
    <genres>
      <genre>Electronic</genre>
    </genres>
    <styles>
      <style>Techno</style>
    </styles>
    <year>2001</year>
    <title>New Soil</title>
    <data_quality>Correct</data_quality>
    <videos>
      <video duration="489" embed="true" src="https://www.youtube.com/watch?v=f05Ai921itM">
        <title>Samuel L - Velvet</title>
        <description>Samuel L - Velvet</description>
      </video>
      <video duration="348" embed="true" src="https://www.youtube.com/watch?v=v23rSPG_StA">
        <title>Samuel L - Danses D'Afrique</title>
        <description>Samuel L - Danses D'Afrique</description>
      </video>
      <video duration="288" embed="true" src="https://www.youtube.com/watch?v=tHo82ha6p40">
        <title>Samuel L - Body N' Soul</title>
        <description>Samuel L - Body N' Soul</description>
      </video>
      <video duration="331" embed="true" src="https://www.youtube.com/watch?v=KDcqzHca5dk">
        <title>Samuel L - Into The Groove</title>
        <description>Samuel L - Into The Groove</description>
      </video>
      <video duration="334" embed="true" src="https://www.youtube.com/watch?v=3DIYjJFl8Dk">
        <title>Samuel L - Soul Syndrome</title>
        <description>Samuel L - Soul Syndrome</description>
      </video>
      <video duration="325" embed="true" src="https://www.youtube.com/watch?v=_o8yZMPqvNg">
        <title>Samuel L - Lush</title>
        <description>Samuel L - Lush</description>
      </video>
      <video duration="346" embed="true" src="https://www.youtube.com/watch?v=JPwwJSc_-30">
        <title>Samuel L - Velvet ( Direct Me )</title>
        <description>Samuel L - Velvet ( Direct Me )</description>
      </video>
    </videos>
  </master>
</masters>'''))
        assert len(exporter.store['Master']) == 1, exporter.store
        master = exporter.store['Master'][0]

        assert master.id == 18500, exporter.store
        assert master.title == u'New Soil', exporter.store
        assert master.year == 2001, exporter.store

        # shouldn't this be an integer
        assert master.main_release == '155102', exporter.store

        assert len(master.styles) == 1, exporter.store
        assert master.styles[0] == u'Techno', exporter.store
        assert len(master.genres) == 1, exporter.store
        assert master.genres[0] == u'Electronic', exporter.store

        assert len(master.images) == 1, exporter.store
        assert len(master.artists) == 5, exporter.store

        # videos are not parsed
        assert getattr(master, 'videos', None) is None, exporter.store


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
