#!/usr/bin/env python
# -*- coding: utf-8 -*-
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

import xml.sax.handler
import xml.sax
import sys
from exporters import make_exporter
import argparse  # in < 2.7 pip install argparse
import gzip

from os import path
from model import ParserStopError
# from collections import deque

# sys.setdefaultencoding('utf-8')
options = None

# http://www.discogs.com/help/voting-guidelines.html
data_quality_values = (
	'Needs Vote',
	'Complete And Correct',
	'Correct',
	'Needs Minor Changes',
	'Needs Major Changes',
	'Entirely Incorrect',
	'Entirely Incorrect Edit'
)


def first_file_match(file_pattern):
	global options
	for f in options.file:
		if file_pattern in f:
			return f
	return None


def parseEntities(parser, exporter, entities, handler_class):
	global options
	entity_file = None
	in_file = first_file_match('_%s.xml' % entities)
	if options.date is not None:
		entity_file = "discogs_%s_%s.xml" % (options.date, entities)
	elif in_file is not None:
		entity_file = in_file

	if entity_file is None:
		# print("No %s file specified." % entities)
		return
	elif not path.exists(entity_file):
		# print("File %s doesn't exist:" % entity_file)
		return

	entityHandler = handler_class(exporter, stop_after=options.n, ignore_missing_tags=options.ignore_unknown_tags)
	parser.setContentHandler(entityHandler)
	try:
		if entity_file.endswith(".gz"):
			_open = gzip.open
		else:
			_open = open
		with _open(entity_file) as f:
			parser.parse(f)
	except ParserStopError as pse:
		print("Parsed %d %s then stopped as requested." % (pse.records_parsed, entities))


def parseArtists(parser, exporter):
	from parsers.discogsartistparser import ArtistHandler
	parseEntities(parser, exporter, 'artists', ArtistHandler)


def parseLabels(parser, exporter):
	from parsers.discogslabelparser import LabelHandler
	parseEntities(parser, exporter, 'labels', LabelHandler)


def parseReleases(parser, exporter):
	from parsers.discogsreleaseparser import ReleaseHandler
	parseEntities(parser, exporter, 'releases', ReleaseHandler)


def parseMasters(parser, exporter):
	from parsers.discogsmasterparser import MasterHandler
	parseEntities(parser, exporter, 'masters', MasterHandler)


def main(argv):
	opt_parser = argparse.ArgumentParser(
		description='Parse discogs release',
		epilog='''
You must specify either -d DATE or some files.
JSON output prints to stdout, any other output requires
that --params is used, e.g.:
--output pgsql
--params "host=localhost dbname=discogs user=pguser"

--output couchdb
--params "http://localhost:5353/"
'''
	)
	opt_parser.add_argument('-n', type=int, help='Number of records to parse', default=0)
	opt_parser.add_argument('-d', '--date', help='Date of release. For example 20110301')
	opt_parser.add_argument('-o', '--output', default='json', help='What to output to')
	opt_parser.add_argument('-p', '--params', help='Parameters for output, e.g. connection string')
	opt_parser.add_argument('-i', '--ignore-unknown-tags', action='store_true', dest='ignore_unknown_tags', help='Do not error out when encountering unknown tags')
	opt_parser.add_argument('-q', '--quality', dest='data_quality', help='Comma-separated list of permissable data_quality values.')
	opt_parser.add_argument('file', nargs='*', help='Specific file(s) to import. Default is to parse artists, labels, releases matching -d')
	global options
	options = opt_parser.parse_args(argv)

	if options.date is None and len(options.file) == 0:
		opt_parser.print_help()
		sys.exit(1)

	exporter = make_exporter(options)
	parser = xml.sax.make_parser()
	try:
		parseArtists(parser, exporter)
		parseLabels(parser, exporter)
		parseReleases(parser, exporter)
		parseMasters(parser, exporter)
	finally:
		exporter.finish(completely_done=True)

if __name__ == "__main__":
	main(sys.argv[1:])
