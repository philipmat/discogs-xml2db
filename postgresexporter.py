#!/usr/bin/python
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

# import psycopg2
import uuid
import sys


def untuple(what):
	if type(what) is tuple:
		return list(what)
	else:
		return [what]


def flatten(what):
	return list([i for sub in what for i in untuple(sub)])


class PostgresExporter(object):
	class ExecuteError(Exception):
		def __init__(self, args):
			self.args = args

	def __init__(self, connection_string, data_quality):
		self.formatNames = {}
		self.connect(connection_string)
		self.min_data_quality = data_quality

	def connect(self, connection_string):
		import psycopg2
		try:
			self.conn = psycopg2.connect(connection_string)
			self.cur = self.conn.cursor()
			self.conn.set_isolation_level(0)
		except psycopg2.Error as e:
			print("%s" % (e.args))
			sys.exit()

	def good_quality(self, what):
		if len(self.min_data_quality):
			return what.data_quality.lower() in self.min_data_quality
		return True

	def execute(self, query, values):
		import psycopg2
		try:
			self.cur.execute(query, values)
		except psycopg2.Error as e:
			try:
				print("Error executing: %s" % self.cur.mogrify(query, values))
			except TypeError:
				print("Error executing: %s" % query)
			raise PostgresExporter.ExecuteError(e.args)

	def finish(self, completely_done=False):
		self.conn.commit()
		if completely_done:
			self.cur.close()

	@staticmethod
	def buildEscapedQuery(table, columns, values):
		return "INSERT INTO {table}({columns}) VALUES ({escaped});".format(
			table=table,
			columns=", ".join(columns),
			escaped=", ".join(["%s"] * len(values)))

	def runQuery(self, table, columns, values):
		query = self.buildEscapedQuery(table, columns, values)
		try:
			self.execute(query, values)
		except PostgresExporter.ExecuteError as e:
			print("%s" % (e.args))
			raise

	def storeLabel(self, label):
		if not self.good_quality(label):
			return
		values = []
		columns = []
		for attr, column in [
				('id', 'id'),
				('name', 'name')]:
			values.append(getattr(label, attr))
			columns.append(column)

		for attr, column in [
				('contactinfo', 'contactinfo'),
				('profile', 'profile'),
				('parentLabel', 'parent_label'),
				('urls', 'urls'),
				('sublabels', 'sublabels')]:
			value = getattr(label, attr)
			if len(value) != 0:
				values.append(value)
				columns.append(column)

		try:
			self.runQuery('label', columns, values)
		except PostgresExporter.ExecuteError as e:
			print("%s" % (e.args))
			return

		for img in label.images:
			values = (img.imageType, img.height, img.width, label.id)
			self.runQuery('labels_images',
				['type', 'height', 'width', 'label_id'], values)

	def storeArtist(self, artist):
		if not self.good_quality(artist):
			return
		values = []
		columns = []
		for attr, column in [
				('id', 'id'),
				('name', 'name')]:
			values.append(getattr(artist, attr))
			columns.append(column)

		for attr, column in [
				('realname', 'realname'),
				('profile', 'profile'),
				('namevariations', 'namevariations'),
				('urls', 'urls'),
				('aliases', 'aliases'),
				('groups', 'groups'),
				('members', 'members')]:
			value = getattr(artist, attr)
			if len(value) != 0:
				values.append(value)
				columns.append(column)

		try:
			self.runQuery('artist', columns, values)
		except PostgresExporter.ExecuteError as e:
			print("%s" % (e.args))
			return

		for img in artist.images:
			values = (img.imageType, img.height, img.width, artist.id)
			self.runQuery('artists_images',
				['type', 'height', 'width', 'artist_id'], values)

	def storeRelease(self, release):
		if not self.good_quality(release):
			return
		values = []
		columns = []
		for attr, column in [
				('id', 'id'),
				('title', 'title'),
				('status', 'status'),
				('barcode', 'barcode'),]:
			values.append(getattr(release, attr))
			columns.append(column)

		for attr, column in [
				('master_id', 'master_id'),
				('country', 'country'),
				('released', 'released'),
				('notes', 'notes'),
				('genres', 'genres'),
				('styles', 'styles')]:
			value = getattr(release, attr)
			if len(value) != 0:
				values.append(value)
				columns.append(column)

		try:
			self.runQuery('release', columns, values)
		except PostgresExporter.ExecuteError as e:
			print("%s" % (e.args))
			return

		for img in release.images:
			values = (img.imageType, img.height, img.width, artist.id)
			self.runQuery('releases_images',
				['type', 'height', 'width', 'release_id'], values)

		fmt_order = 0
		for fmt in release.formats:
			fmt_order = fmt_order + 1
			if len(release.formats) != 0:
				if fmt.name not in self.formatNames:
					self.formatNames[fmt.name] = True
					try:
						self.runQuery('format', ['name'], (fmt.name, ))
					except PostgresExporter.ExecuteError as e:
						print("%s" % (e.args))
				values = (release.id, fmt_order, fmt.name, fmt.qty, fmt.descriptions)
				self.runQuery('releases_formats',
					['release_id', 'position', 'format_name', 'qty', 'descriptions'], values)

		labelQuery = "INSERT INTO releases_labels(release_id, label, catno) VALUES(%s,%s,%s);"
		for lbl in release.labels:
			self.execute(labelQuery, (release.id, lbl.name, lbl.catno))

		release_artist_order = 0
		for aj in release.artistJoins:
			release_artist_order = release_artist_order + 1
			query = "INSERT INTO releases_artists(release_id, position, artist_id, artist_name, join_relation, anv)  VALUES(%s, %s, %s, %s, %s, %s);"
			self.execute(query, (release.id, release_artist_order, aj.artist_id, aj.artist_name, aj.join_relation, aj.anv))

		for extr in release.extraartists:
			for role in extr.roles:
				self.execute(
					"INSERT INTO releases_extraartists(release_id, artist_id, artist_name, role, anv) VALUES(%s, %s, %s, %s, %s);",
					(release.id, extr.artist_id, extr.artist_name, role, extr.anv))

		trackno = 0
		for trk in release.tracklist:
			trackid = str(uuid.uuid4())
			trackno = trackno + 1
			self.execute(
				"INSERT INTO track(release_id, title, duration, position, track_id, trackno) VALUES(%s,%s,%s,%s,%s,%s);",
				(release.id, trk.title, trk.duration, trk.position, trackid, trackno))

			track_artist_order = 0
			for aj in trk.artistJoins:
				track_artist_order = track_artist_order + 1
				query = "INSERT INTO tracks_artists(track_id, position, artist_name,artist_id, join_relation, anv) VALUES(%s, %s, %s, %s, %s, %s);"
				self.execute(query, (trackid, track_artist_order, aj.artist_name, aj.artist_id, aj.join_relation, aj.anv))

			for extr in trk.extraartists:
				for role in extr.roles:
					self.execute(
						"INSERT INTO tracks_extraartists(track_id, artist_id, artist_name, role, anv) VALUES(%s, %s, %s, %s, %s);",
						(trackid, extr.artist_id, extr.artist_name, role, extr.anv))

	def storeMaster(self, master):
		if not self.good_quality(master):
			return

		values = []
		values.append(master.id)
		values.append(master.title)
		values.append(master.main_release)
		columns = "id, title, main_release"

		if master.year:
			values.append(master.year)
			columns += ",year"
		if len(master.notes) != 0:
			values.append(master.notes)
			columns += ",notes"
		if len(master.genres) != 0:
			values.append(master.genres)
			columns += ",genres"
		if len(master.styles) != 0:
			values.append(master.styles)
			columns += ",styles"

		# INSERT INTO DATABASE
		escapeStrings = ''
		for counter in range(1, len(columns.split(","))):
			escapeStrings = escapeStrings + ",%s"
		escapeStrings = '(%s' + escapeStrings + ')'
		# print(values)
		query = "INSERT INTO master(" + columns + ") VALUES" + escapeStrings + ";"
		# print(query)
		try:
			self.execute(query, values)
		except PostgresExporter.ExecuteError as e:
			print("%s" % (e.args))
			return

		for img in master.images:
			self.execute("INSERT INTO masters_images(type, height, width, master_id) VALUES(%s,%s,%s,%s);", (img.imageType, img.height, img.width, master.id))

		if len(master.artists) > 1:
			for artist in master.artists:
				query = "INSERT INTO masters_artists(master_id, artist_name) VALUES(%s,%s);"
				self.execute(query, (master.id, artist))
			for aj in master.artistJoins:
				query = """INSERT INTO masters_artists_joins
												(master_id, join_relation, artist1, artist2)
												VALUES(%s,%s,%s,%s);"""
				artistIdx = master.artists.index(aj.artist1) + 1
				# The last join relation is not between artists but instead
				# something like "Bob & Alice 'PRESENTS' - Cryptographic Tunes":
				if artistIdx >= len(master.artists):
					values = (master.id, aj.join_relation, '', '')  # join relation is between all artists and the album
				else:
					values = (master.id, aj.join_relation, aj.artist1, master.artists[artistIdx])
				self.execute(query, values)
		else:
			if len(master.artists) == 0:  # use anv if no artist name
				self.execute(
					"INSERT INTO masters_artists(master_id, artist_name) VALUES(%s,%s);",
					(master.id, master.anv))
			else:
				self.execute(
					"INSERT INTO masters_artists(master_id, artist_name) VALUES(%s,%s);",
					(master.id, master.artists[0]))

		for extr in master.extraartists:
			# decide whether to insert flattened composite roles or take the first one from the tuple
			self.execute(
				"INSERT INTO masters_extraartists(master_id, artist_name, roles) VALUES(%s,%s,%s);",
				(master.id, extr.name, map(lambda x: x[0] if type(x) is tuple else x, extr.roles)))
			# (master.id, extr.name, flatten(extr.roles)))


from psycopg2.extensions import adapt, register_adapter
class SQL_LIST(object):
	"""Adapt any iterable to an SQL quotable object."""
	def __init__(self, seq):
		self._seq = seq
		self._conn = None

	def prepare(self, conn):
		self._conn = conn
	def getquoted(self):
		# this is the important line: note how every object in the
		# list is adapted and then how getquoted() is called on it
		pobjs = [adapt(o) for o in self._seq]
		if self._conn is not None:
			for obj in pobjs:
				if hasattr(obj, 'prepare'):
					obj.prepare(self._conn)
		qobjs = [o.getquoted() for o in pobjs]
		return b'{' + b', '.join(qobjs) + b'}'

	def __str__(self):
		return str(self.getquoted())

class PostgresConsoleDumper(PostgresExporter):

	def __init__(self, connection_string, data_quality=None):
		super(PostgresConsoleDumper, self).__init__(connection_string, data_quality)
		register_adapter(list, SQL_LIST)

	def execute(self, query, params):
		print(self.cur.mogrify(query, params).decode())

	def finish(self, completely_done=False):
		from psycopg2._psycopg import List
		register_adapter(list, List)
