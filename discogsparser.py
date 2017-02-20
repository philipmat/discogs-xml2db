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
import jsonexporter
import argparse  # in < 2.7 pip install argparse
import gzip

from os import path
from model import ParserStopError
from collections import deque

# sys.setdefaultencoding('utf-8')
options = None

exporters = {
	'json': 'jsonexporter.JsonConsoleExporter',
	'pgsql': 'postgresexporter.PostgresExporter',
	'pgdump': 'postgresexporter.PostgresConsoleDumper',
	'couch': 'couchdbexporter.CouchDbExporter',
	'mongo': 'mongodbexporter.MongoDbExporter',
}

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


def parseArtists(parser, exporter):
	global options
	artist_file = None
	in_file = first_file_match('_artists.xml')
	if options.date is not None:
		artist_file = "discogs_%s_artists.xml" % options.date
	elif in_file is not None:
		artist_file = in_file

	if artist_file is None:
		# print("No artist file specified.")
		return
	elif not path.exists(artist_file):
		# print("File %s doesn't exist:" % artist_file)
		return

	from discogsartistparser import ArtistHandler
	artistHandler = ArtistHandler(exporter, stop_after=options.n, ignore_missing_tags=options.ignore_unknown_tags)
	parser.setContentHandler(artistHandler)
	try:
		if artist_file.endswith(".gz"):
			with gzip.open(artist_file) as f:
				parser.parse(f)
		else:
			parser.parse(artist_file)
	except ParserStopError as pse:
		print("Parsed %d artists then stopped as requested." % pse.records_parsed)
# 	except model.ParserStopError as pse22:
# 		print("Parsed %d artists then stopped as requested." % pse.records_parsed)
# 	except Exception as ex:
# 		print("Raised unknown error")
# 		print(type(ex))


def parseLabels(parser, exporter):
	global options
	label_file = None
	in_file = first_file_match('_labels.xml')
	if options.date is not None:
		label_file = "discogs_%s_labels.xml" % options.date
	elif in_file is not None:
		label_file = in_file

	if label_file is None:
		# print("No label file specified.")
		return
	elif not path.exists(label_file):
		# print("File %s doesn't exist:" % label_file)
		return

	from discogslabelparser import LabelHandler
	labelHandler = LabelHandler(exporter, stop_after=options.n, ignore_missing_tags=options.ignore_unknown_tags)
	parser.setContentHandler(labelHandler)
	try:
		if label_file.endswith(".gz"):
			with gzip.open(label_file) as f:
				parser.parse(f)
		else:
			parser.parse(label_file)
	except ParserStopError as pse:
		print("Parsed %d labels then stopped as requested." % pse.records_parsed)


def parseReleases(parser, exporter):
	global options
	release_file = None
	in_file = first_file_match('_releases.xml')
	if options.date is not None:
		release_file = "discogs_%s_releases.xml" % options.date
	elif in_file is not None:
		release_file = in_file

	if release_file is None:
		# print("No release file specified.")
		return
	elif not path.exists(release_file):
		# print("File %s doesn't exist:" % release_file)
		return

	from discogsreleaseparser import ReleaseHandler
	releaseHandler = ReleaseHandler(exporter, stop_after=options.n, ignore_missing_tags=options.ignore_unknown_tags)
	parser.setContentHandler(releaseHandler)
	try:
		if release_file.endswith(".gz"):
			with gzip.open(release_file) as f:
				parser.parse(f)
		else:
			parser.parse(release_file)
	except ParserStopError as pse:
		print("Parsed %d releases then stopped as requested." % pse.records_parsed)


def parseMasters(parser, exporter):
	global options
	master_file = None
	in_file = first_file_match('_masters.xml')
	if options.date is not None:
		master_file = "discogs_%s_masters.xml" % options.date
	elif in_file is not None:
		master_file = in_file

	if master_file is None:
		# print("No masters file specified.")
		return
	elif not path.exists(master_file):
		# print("File %s doesn't exist:" % master_file)
		return

	from discogsmasterparser import MasterHandler
	masterHandler = MasterHandler(exporter, stop_after=options.n, ignore_missing_tags=options.ignore_unknown_tags)
	parser.setContentHandler(masterHandler)
	try:
		if master_file.endswith(".gz"):
			with gzip.open(master_file) as f:
				parser.parse(f)
		else:
			parser.parse(master_file)
	except ParserStopError as pse:
		print("Parsed %d masters then stopped as requested." % pse.records_parsed)


def select_exporter(options):
	global exporters
	if options.output is None:
		return exporters['json']

	if options.output in exporters:
		return exporters[options.output]
	# should I be throwing an exception here?
	return exporters['json']


def make_exporter(options):
	exp_module = select_exporter(options)

	parts = exp_module.split('.')
	m = __import__('.'.join(parts[:-1]))
	for p in parts[1:]:
		m = getattr(m, p)

	data_quality = list(x.strip().lower() for x in (options.data_quality or '').split(',') if x)
	return m(options.params, data_quality=data_quality)


def main(argv):
	global exporters
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
	opt_parser.add_argument('-o', '--output', choices=exporters.keys(), default='json', help='What to output to')
	opt_parser.add_argument('-p', '--params', help='Parameters for output, e.g. connection string')
	opt_parser.add_argument('-i', '--ignore-unknown-tags', action='store_true', dest='ignore_unknown_tags', help='Do not error out when encountering unknown tags')
	opt_parser.add_argument('-q', '--quality', dest='data_quality', help='Comma-separated list of permissable data_quality values.')
	opt_parser.add_argument('file', nargs='*', help='Specific file(s) to import. Default is to parse artists, labels, releases matching -d')
	global options
	options = opt_parser.parse_args(argv)
	print(options)

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
