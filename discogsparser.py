#!/usr/bin/python
# -*- coding: utf-8 -*-
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

import xml.sax.handler
import xml.sax
import sys
reload(sys)
sys.setdefaultencoding('utf-8')
from discogsartistparser import ArtistHandler
from discogslabelparser import LabelHandler
from discogsreleaseparser import ReleaseHandler

def parseArtists(parser,release):
    artistHandler = ArtistHandler()
    parser.setContentHandler(artistHandler)
    parser.parse("discogs_%s_artists.xml" % release)

def parseLabels(parser, release):
    labelHandler = LabelHandler()
    parser.setContentHandler(labelHandler)
    parser.parse("discogs_%s_labels.xml" % release) 

def parseReleases(parser, release):
    releaseHandler = ReleaseHandler()
    parser.setContentHandler(releaseHandler)
    parser.parse("discogs_%s_releases.xml" % release)

def usage():
  print "Usage: python discogsparser.py relase, where release is for example 20091101"
  sys.exit()

def main(argv):
    if len(argv) == 0 or len(argv[0]) != 8:
      usage()
    try:
      int(argv[0])
    except ValueError:
      usage() 
      sys.exit()
    release = argv[0]

    parser = xml.sax.make_parser()
    parseArtists(parser, release)
    parseLabels(parser, release)
    parseReleases(parser, release)

if __name__ == "__main__":
    main(sys.argv[1:])

