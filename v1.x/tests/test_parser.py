# -*- coding: utf-8 -*-
import gzip
from io import BytesIO
from pprint import pprint
import os
import xml.sax

from parsers.discogsartistparser import ArtistHandler
from parsers.discogslabelparser import LabelHandler
from parsers.discogsmasterparser import MasterHandler
from parsers.discogsreleaseparser import ReleaseHandler

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
        pprint(exporter.store)
        assert len(exporter.store['Artist']) == 1
        artist = exporter.store['Artist'][0]
        pprint(artist.__dict__)

        assert artist.id == 4678
        assert artist.name == u'The Persuader'
        assert artist.realname == u'Jesper Dahlbäck'

        assert len(artist.aliases) == 7
        assert artist.aliases[0] == u'Dick Track'

        assert len(artist.namevariations) == 2
        assert artist.namevariations[0] == u'Persuader'

        assert len(artist.groups) == 16
        assert artist.groups[0] == u'Christian Smith & John Selway'

        assert len(artist.urls) == 3
        assert artist.urls[0] == u'http://www.selwaymusic.net'

        assert len(artist.images) == 1
        assert len(artist.profile) > 0


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
        pprint(exporter.store)
        assert len(exporter.store['Label']) == 1
        label = exporter.store['Label'][0]
        pprint(label.__dict__)

        assert label.id == 42
        assert label.name == u'Planet E'

        assert len(label.sublabels) == 7
        assert label.sublabels[0] == u'Antidote (4)'

        assert len(label.urls) == 13
        assert label.urls[0] == u'http://planet-e.net/'

        assert len(label.profile) > 0
        assert len(label.contactinfo) > 0


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
        pprint(exporter.store)
        assert len(exporter.store['Master']) == 1
        master = exporter.store['Master'][0]
        pprint(master.__dict__)

        assert master.id == 18500
        assert master.title == u'New Soil'
        assert master.year == 2001

        # shouldn't this be an integer
        assert master.main_release == '155102'

        assert len(master.styles) == 1
        assert master.styles[0] == u'Techno'
        assert len(master.genres) == 1
        assert master.genres[0] == u'Electronic'

        assert len(master.images) == 1
        assert len(master.artists) == 5

        # videos are not parsed
        assert getattr(master, 'videos', None) is None


class TestReleaseParser(object):

    def test_basic_unicode(self):
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(b'''<releases>
  <release id="64" status="Accepted">
    <images>
      <image height="600" type="primary" uri="" uri150="" width="600"/>
      <image height="600" type="secondary" uri="" uri150="" width="600"/>
      <image height="600" type="secondary" uri="" uri150="" width="600"/>
      <image height="600" type="secondary" uri="" uri150="" width="600"/>
    </images>
    <artists>
      <artist>
        <id>1</id>
        <name>The Persuader</name>
        <anv/>
        <join>,</join>
        <role/>
        <tracks/>
      </artist>
    </artists>
    <title>Stockholm</title>
    <labels>
      <label catno="SK032" name="Svek"/>
    </labels>
    <extraartists>
      <artist>
        <id>239</id>
        <name>Jesper Dahlb&#xE4;ck</name>
        <anv/>
        <join/>
        <role>Music By [All Tracks By]</role>
        <tracks/>
      </artist>
    </extraartists>
    <formats>
      <format name="Vinyl" qty="2" text="">
        <descriptions>
          <description>12"</description>
          <description>33 &#x2153; RPM</description>
        </descriptions>
      </format>
    </formats>
    <genres>
      <genre>Electronic</genre>
    </genres>
    <styles>
      <style>Deep House</style>
    </styles>
    <country>Sweden</country>
    <released>1999-03-00</released>
    <notes>The song titles are the names of six of Stockholm's 82 districts.

Title on label: - Stockholm -

Recorded at the Globe Studio, Stockholm

FAX: +46 8 679 64 53

</notes>
    <master_id>713738</master_id>
    <data_quality>Needs Vote</data_quality>
    <tracklist>
      <track>
        <position>A</position>
        <title>&#xD6;stermalm</title>
        <duration>4:45</duration>
      </track>
      <track>
        <position>B1</position>
        <title>Vasastaden</title>
        <duration>6:11</duration>
      </track>
      <track>
        <position>B2</position>
        <title>Kungsholmen</title>
        <duration>2:49</duration>
      </track>
      <track>
        <position>C1</position>
        <title>S&#xF6;dermalm</title>
        <duration>5:38</duration>
      </track>
      <track>
        <position>C2</position>
        <title>Norrmalm</title>
        <duration>4:52</duration>
      </track>
      <track>
        <position>D</position>
        <title>Gamla Stan</title>
        <duration>5:16</duration>
      </track>
    </tracklist>
    <identifiers>
      <identifier description="A-Side Runout" type="Matrix / Runout" value="MPO SK 032 A1"/>
      <identifier description="B-Side Runout" type="Matrix / Runout" value="MPO SK 032 B1"/>
      <identifier description="C-Side Runout" type="Matrix / Runout" value="MPO SK 032 C1"/>
      <identifier description="D-Side Runout" type="Matrix / Runout" value="MPO SK 032 D1"/>
      <identifier description="Only On A-Side Runout" type="Matrix / Runout" value="G PHRUPMASTERGENERAL T27 LONDON"/>
    </identifiers>
    <videos>
      <video duration="290" embed="true" src="https://www.youtube.com/watch?v=AHuQWcylaU4">
        <title>The Persuader (Jesper Dahlb&#xE4;ck) - &#xD6;stermalm</title>
        <description>The Persuader (Jesper Dahlb&#xE4;ck) - &#xD6;stermalm</description>
      </video>
      <video duration="380" embed="true" src="https://www.youtube.com/watch?v=5rA8CTKKEP4">
        <title>The Persuader - Vasastaden</title>
        <description>The Persuader - Vasastaden</description>
      </video>
      <video duration="335" embed="true" src="https://www.youtube.com/watch?v=QVdDhOnoR8k">
        <title>The Persuader-Stockholm-Sodermalm</title>
        <description>The Persuader-Stockholm-Sodermalm</description>
      </video>
      <video duration="289" embed="true" src="https://www.youtube.com/watch?v=hy47qgyJeG0">
        <title>The Persuader - Norrmalm</title>
        <description>The Persuader - Norrmalm</description>
      </video>
      <video duration="322" embed="true" src="https://www.youtube.com/watch?v=DubEDc1qvF8">
        <title>The Persuader - Gamla Stan</title>
        <description>The Persuader - Gamla Stan</description>
      </video>
    </videos>
    <companies>
      <company>
        <id>271046</id>
        <name>The Globe Studios</name>
        <catno/>
        <entity_type>23</entity_type>
        <entity_type_name>Recorded At</entity_type_name>
        <resource_url>http://api.discogs.com/labels/271046</resource_url>
      </company>
      <company>
        <id>56025</id>
        <name>MPO</name>
        <catno/>
        <entity_type>17</entity_type>
        <entity_type_name>Pressed By</entity_type_name>
        <resource_url>http://api.discogs.com/labels/56025</resource_url>
      </company>
    </companies>
  </release>
</releases>'''))
        pprint(exporter.store)
        assert len(exporter.store['Release']) == 1
        release = exporter.store['Release'][0]
        pprint(release.__dict__)

        assert release.id == 64
        assert release.title == u'Stockholm'
        assert release.master_id == 713738

        assert release.country == u'Sweden'
        assert release.released == u'1999-03-00'

        assert len(release.styles) == 1
        assert release.styles[0] == u'Deep House'
        assert len(release.genres) == 1
        assert release.genres[0] == u'Electronic'

        assert len(release.images) == 4

        assert len(release.labels) == 1
        label = release.labels[0]
        assert label.name == u'Svek'
        assert label.catno == 'SK032'

        assert len(release.artistJoins) == 1
        aj = release.artistJoins[0]
        assert aj.artist_id == u'1'  # FIXME: should be an integer, no?
        assert aj.artist_name == u'The Persuader'
        assert aj.join_relation == u','
        assert aj.anv is None

        assert len(release.extraartists) == 1
        ea = release.extraartists[0]
        assert ea.artist_id == u'239'  # FIXME: should be an integer, no?
        assert ea.artist_name == u'Jesper Dahlbäck'
        assert ea.roles == [(u'Music By', u'All Tracks By')]
        assert ea.anv is None

        assert len(release.formats) == 1
        f = release.formats[0]
        assert f.name == u'Vinyl'
        assert f.qty == u'2'
        assert f.text == u''
        assert f.descriptions == [u'12"', u'33 ⅓ RPM']

        assert len(release.notes) > 0

        # FIXME: identifiers are not parsed
        assert getattr(release, 'identifiers', None) is None

        # FIXME: videos are not parsed
        assert getattr(release, 'videos', None) is None

        assert len(release.companies) == 2
        company = release.companies[0]
        # FIXME: shouldn't this be an integer?
        assert company.id == u'271046'
        assert company.name == u'The Globe Studios'
        assert company.catno == u''
        assert company.type == u'23'
        assert company.type_name == u'Recorded At'

        assert len(release.tracklist) == 6
        track = release.tracklist[0]
        assert track.duration == '4:45'
        assert track.artistJoins == []
        assert track.extraartists == []
        assert track.position == u'A'
        assert track.title == u'Östermalm'

    def test_subtracks_001(self):
        xmlinput = b'''<releases>
  <release id="723" status="Accepted">
    <artists>
      <artist>
        <id>1100</id>
        <name>Blake Baxter</name>
        <anv/>
        <join/>
        <role/>
        <tracks/>
      </artist>
    </artists>
    <tracklist>
      <track>
        <position>1</position>
        <title>Ghost</title>
        <duration>5:24</duration>
        <extraartists>
          <artist>
            <id>3836</id>
            <name>Johnny Klimek</name>
            <anv/>
            <join/>
            <role>Written-By</role>
            <tracks/>
          </artist>
        </extraartists>
      </track>
      <track>
        <position>2</position>
        <title>The Warning</title>
        <duration>4:20</duration>
      </track>
      <track>
        <position>3</position>
        <title>One Mo Time (Mix 1)</title>
        <duration>4:59</duration>
      </track>
      <track>
        <position>4</position>
        <title>Laser 101</title>
        <duration>4:49</duration>
      </track>
      <track>
        <position>5</position>
        <title>Ex-</title>
        <duration>4:55</duration>
        <extraartists>
          <artist>
            <id>3836</id>
            <name>Johnny Klimek</name>
            <anv/>
            <join/>
            <role>Written-By</role>
            <tracks/>
          </artist>
        </extraartists>
      </track>
      <track>
        <position>6</position>
        <title>Dark B&#xE4;sse</title>
        <duration>4:56</duration>
      </track>
      <track>
        <position>7</position>
        <title>Adrenalin</title>
        <duration>5:29</duration>
      </track>
      <track>
        <position>8</position>
        <title>One Mo Time (Mix 2)</title>
        <duration>5:07</duration>
      </track>
      <track>
        <position>9</position>
        <title>Frequency Old Skool</title>
        <duration>6:06</duration>
      </track>
      <track>
        <position>10</position>
        <title>Laugh And Dog</title>
        <duration>4:24</duration>
      </track>
      <track>
        <position/>
        <title>Dreammaker</title>
        <duration>13:04</duration>
        <sub_tracks>
          <track>
            <position>11.a</position>
            <title>909 Shuffle</title>
            <duration>3:10</duration>
          </track>
          <track>
            <position>11.b</position>
            <title>Adrenalin (Ciborg Rmx)</title>
            <duration>5:38</duration>
          </track>
          <track>
            <position>11.c</position>
            <title>Laser 101 Rmx</title>
            <duration>4:26</duration>
          </track>
        </sub_tracks>
      </track>
    </tracklist>
  </release>
</releases>'''
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(xmlinput))

        pprint(exporter.store)
        assert len(exporter.store['Release']) == 1
        release = exporter.store['Release'][0]
        pprint(release.__dict__)

        trackids = [t.position for t in release.tracklist]
        # FIXME: there's a bug here: subtracks '11.a' and '11.b' are missed
        # assert len(release.tracklist) == 13
        # assert trackids == ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11.a', '11.b', '11.c']
        assert len(release.tracklist) == 11
        assert trackids == ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11.c']

    def test_subtracks_002(self):
        xmlinput = b'''<releases>
  <release id="779" status="Accepted">
    <artists>
      <artist>
        <id>1100</id>
        <name>Blake Baxter</name>
        <anv/>
        <join/>
        <role/>
        <tracks/>
      </artist>
    </artists>
    <tracklist>
      <track>
        <position/>
        <title>Remnants Of What Once Was</title>
        <duration>9:51</duration>
        <sub_tracks>
          <track>
            <position>1a</position>
            <title>The Hollow Men</title>
            <duration/>
          </track>
          <track>
            <position>1b</position>
            <title>Ice</title>
            <duration/>
          </track>
        </sub_tracks>
      </track>
      <track>
        <position/>
        <title>Black Jackal Throwbacks</title>
        <duration>11:46</duration>
        <sub_tracks>
          <track>
            <position>2a</position>
            <title>Part 1</title>
            <duration/>
          </track>
          <track>
            <position>2b</position>
            <title>Part 2</title>
            <duration/>
          </track>
          <track>
            <position>2c</position>
            <title>Part 3</title>
            <duration/>
          </track>
        </sub_tracks>
      </track>
      <track>
        <position/>
        <title>Returning To The Purity Of Current</title>
        <duration>8:23</duration>
        <sub_tracks>
          <track>
            <position>3a</position>
            <title>Part 1</title>
            <duration/>
          </track>
          <track>
            <position>3b</position>
            <title>Part 2</title>
            <duration/>
          </track>
        </sub_tracks>
      </track>
      <track>
        <position/>
        <title>At The Heart Of It All</title>
        <duration>10:43</duration>
        <sub_tracks>
          <track>
            <position>4a</position>
            <title>Part 1</title>
            <duration/>
          </track>
          <track>
            <position>4b</position>
            <title>Part 2</title>
            <duration/>
          </track>
        </sub_tracks>
      </track>
    </tracklist>
  </release>
</releases>'''
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(xmlinput))

        pprint(exporter.store)
        assert len(exporter.store['Release']) == 1
        release = exporter.store['Release'][0]
        pprint(release.__dict__)

        trackids = [t.position for t in release.tracklist]
        # FIXME: there's a bug in the parser here,
        #        it only considers the last subtrack for each track
        # assert len(release.tracklist) == 9
        # assert trackids == ['1a', '1b', '2a', '2b', '2c', '3a', '3b', '4a', '4b']
        assert len(release.tracklist) == 4
        assert trackids == ['1b', '2c', '3b', '4b']

    def test_subtracks_003(self):
        xmlinput = b'''<releases>
  <release id="870" status="Accepted">
    <artists>
      <artist>
        <id>1100</id>
        <name>Blake Baxter</name>
        <anv/>
        <join/>
        <role/>
        <tracks/>
      </artist>
    </artists>
    <tracklist>
      <track>
        <position/>
        <title>The Box</title>
        <duration/>
        <sub_tracks>
          <track>
            <position>1</position>
            <title>Radio Edit</title>
            <duration>4:13</duration>
          </track>
          <track>
            <position>2</position>
            <title>Untitled</title>
            <duration>7:46</duration>
          </track>
          <track>
            <position>3</position>
            <title>Untitled</title>
            <duration>8:40</duration>
          </track>
          <track>
            <position>4</position>
            <title>Vocal Reprise</title>
            <duration>7:36</duration>
            <extraartists>
              <artist>
                <id>15589</id>
                <name>Alison Goldfrapp</name>
                <anv/>
                <join/>
                <role>Vocals</role>
                <tracks/>
              </artist>
              <artist>
                <id>72489</id>
                <name>Grant Fulton</name>
                <anv/>
                <join/>
                <role>Vocals, Written-By [Vocals]</role>
                <tracks/>
              </artist>
              <artist>
                <id>1839120</id>
                <name>Pete Mauder (2)</name>
                <anv>Peter Mauder</anv>
                <join/>
                <role>Written-By [Vocals]</role>
                <tracks/>
              </artist>
            </extraartists>
          </track>
        </sub_tracks>
      </track>
    </tracklist>
  </release>
</releases>'''
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(xmlinput))

        pprint(exporter.store)
        assert len(exporter.store['Release']) == 1
        release = exporter.store['Release'][0]
        pprint(release.__dict__)

        for t in release.tracklist:
            pprint(t.__dict__)
        trackids = [t.position for t in release.tracklist]
        # FIXME: there's a bug in the parser here,
        #        it only considers the last subtrack
        # assert len(release.tracklist) == 4
        # assert trackids == ['1', '2', '3', '4']
        assert len(release.tracklist) == 1
        assert trackids == ['4']

    def test_ignore_no_artist(self):
        xmlinput = b'''<releases>
<release id="723" status="Accepted">
    <tracklist>
      <track>
        <position>1</position>
        <title>Ghost</title>
        <duration>5:24</duration>
        <extraartists>
          <artist>
            <id>3836</id>
            <name>Johnny Klimek</name>
            <anv/>
            <join/>
            <role>Written-By</role>
            <tracks/>
          </artist>
        </extraartists>
      </track>
    </tracklist>
  </release>
</releases>'''
        parser = xml.sax.make_parser()
        exporter = AppendingExporter()
        entityHandler = ReleaseHandler(exporter)
        parser.setContentHandler(entityHandler)
        parser.parse(BytesIO(xmlinput))

        assert len(exporter.store['Release']) == 0


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
