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

#import psycopg2
import uuid
import sys

class PostgresExporter(object):
	class ExecuteError(Exception):
		def __init__(self, args):
			self.args = args


	def __init__(self, connection_string):
		self.formatNames = {}
		self.imgUris = {}
		self.connect(connection_string)
	
	def connect(self, connection_string):
		import psycopg2
		try:
			self.conn = psycopg2.connect(connection_string)
			self.cur = self.conn.cursor()
			self.conn.set_isolation_level(0)
		except psycopg2.Error, e:
			print "%s" % (e.args)
			sys.exit()

	def execute(self, query, values):
		import psycopg2
		try:
			self.cur.execute(query, values)
		except psycopg2.Error as e:
			try:
				print "Error executing: %s" % self.cur.mogrify(query, values)
			except TypeError:
				print "Error executing: %s" % query
			raise PostgresExporter.ExecuteError(e.args)

	def finish(self, completely_done = False):
		self.conn.commit()
		if completely_done:
			self.cur.close()


	def storeLabel(self, label):
		values = []
		values.append(label.id)
		values.append(label.name)
		columns = "id,name"

		if len(label.contactinfo) != 0:
			values.append(label.contactinfo)
			columns += ",contactinfo"
		if len(label.profile) != 0:
			values.append(label.profile)
			columns += ",profile"
		if len(label.parentLabel) != 0:
			values.append(label.parentLabel)
			columns += ",parent_label"
		if len(label.urls) != 0:
			values.append(label.urls)
			columns += ",urls"
		if len(label.sublabels) != 0:
			values.append(label.sublabels)
			columns += ",sublabels"

		escapeStrings = ''
		for counter in xrange(1, len(columns.split(","))):
			escapeStrings = escapeStrings + ",%s"
		escapeStrings = '(%s' + escapeStrings + ')'
		#print values
		query = "INSERT INTO label(" + columns + ") VALUES" + escapeStrings + ";"
		#print query
		try:
			self.execute(query, values)
		except PostgresExporter.ExecuteError as e:
			print "%s" % (e.args)
			return
		imgCols = "uri,height,width,type,uri150"
		for img in label.images:
			imgValues = []
			imgValues.append(img.uri)
			imgValues.append(img.height)
			imgValues.append(img.width)
			imgValues.append(img.imageType)
			imgValues.append(img.uri150)
			if len(imgValues) != 0:
				if not img.uri in self.imgUris:
					imgQuery = "INSERT INTO image(" + imgCols + ") VALUES(%s,%s,%s,%s,%s);"
					self.execute(imgQuery, imgValues)
					self.imgUris[img.uri] = True
				self.execute("INSERT INTO labels_images(image_uri, label_id) VALUES(%s,%s);", (img.uri, label.id))


	def storeArtist(self, artist):
		values = []
		value.append(artist.id)
		values.append(artist.name)
		columns = "id,name"

		if len(artist.realname) != 0:
			values.append(artist.realname)
			columns += ",realname"
		if len(artist.profile) != 0:
			values.append(artist.profile)
			columns += ",profile"
		if len(artist.namevariations) != 0:
			values.append(artist.namevariations)
			columns += ",namevariations"
		if len(artist.urls) != 0:
			values.append(artist.urls)
			columns += ",urls"
		if len(artist.aliases) != 0:
			values.append(artist.aliases)
			columns += ",aliases"
		if len(artist.groups) != 0:
			values.append(artist.groups)
			columns += ",groups"
		if len(artist.members) != 0:
			values.append(artist.members)
			columns += ",members"

		escapeStrings = ''
		for counter in xrange(1, len(columns.split(","))):
			escapeStrings = escapeStrings + ",%s"
		escapeStrings = '(%s' + escapeStrings + ')'
		#print values
		query = "INSERT INTO artist(" + columns + ") VALUES" + escapeStrings + ";"
		#print query
		try:
			self.execute(query, values)
		except PostgresExporter.ExecuteError, e:
			print "%s" % (e.args)
			return

		imgCols = "uri,height,width,type,uri150"
		for img in artist.images:
			imgValues = []
			imgValues.append(img.uri)
			imgValues.append(img.height)
			imgValues.append(img.width)
			imgValues.append(img.imageType)
			imgValues.append(img.uri150)
			if len(imgValues) != 0:
				if not img.uri in self.imgUris:
					imgQuery = "INSERT INTO image(" + imgCols + ") VALUES(%s,%s,%s,%s,%s);"
					self.execute(imgQuery, imgValues)
					self.imgUris[img.uri] = True
				self.execute("INSERT INTO artists_images(image_uri, artist_id) VALUES(%s,%s);", (img.uri, artist.id))


	def storeRelease(self, release):

		values = []
		values.append(release.id)
		values.append(release.title)
		values.append(release.status)
		columns = "id, title, status"

		if len(release.country) != 0:
			values.append(release.country)
			columns += ",country"
		if len(release.released) != 0:
			values.append(release.released)
			columns += ",released"
		if len(release.notes) != 0:
			values.append(release.notes)
			columns += ",notes"
		if len(release.genres) != 0:
			values.append(release.genres)
			columns += ",genres"
		if len(release.styles) != 0:
			values.append(release.styles)
			columns += ",styles"


		#'''INSERT INTO DATABASE
		escapeStrings = ''
		for counter in xrange(1, len(columns.split(","))):
			escapeStrings = escapeStrings + ",%s"
		escapeStrings = '(%s' + escapeStrings + ')'
		#print values
		query = "INSERT INTO release(" + columns + ") VALUES" + escapeStrings + ";"
		#print query
		try:
			self.execute(query, values)
		except PostgresExporter.ExecuteError, e:
			print "%s" % (e.args)
			return
		imgCols = "uri,height,width,type,uri150"
		for img in release.images:
			imgValues = []
			imgValues.append(img.uri)
			imgValues.append(img.height)
			imgValues.append(img.width)
			imgValues.append(img.imageType)
			imgValues.append(img.uri150)
			if len(imgValues) != 0:
				if not img.uri in self.imgUris:
					self.execute("SELECT uri FROM image WHERE uri=%s;",  (img.uri, ))
					if self.cur is None or len(self.cur.fetchall()) == 0:
						imgQuery = "INSERT INTO image(" + imgCols + ") VALUES(%s,%s,%s,%s,%s);"
						self.execute(imgQuery, imgValues)
					self.imgUris[img.uri] = True
				self.execute("INSERT INTO releases_images(image_uri, release_id) VALUES(%s,%s);",
						(img.uri, release.id))
		for fmt in release.formats:
			if len(release.formats) != 0:
				if not fmt.name in self.formatNames:
					self.formatNames[fmt.name] = True
					try:
						self.execute("INSERT INTO format(name) VALUES(%s);", (fmt.name, ))
					except PostgresExporter.ExecuteError, e:
						print "%s" % (e.args)
				query = "INSERT INTO releases_formats(release_id, format_name, qty, descriptions) VALUES(%s,%s,%s,%s);"
				self.execute(query, (release.id, fmt.name, fmt.qty, fmt.descriptions))
		labelQuery = "INSERT INTO releases_labels(release_id, label, catno) VALUES(%s,%s,%s);"
		for lbl in release.labels:
			self.execute(labelQuery, (release.id, lbl.name, lbl.catno))

		if len(release.artists) > 1:
			for artist in release.artists:
				query = "INSERT INTO releases_artists(release_id, artist_name) VALUES(%s,%s);"
				self.execute(query, (release.id, artist))
			for aj in release.artistJoins:
				query = """INSERT INTO releases_artists_joins
												(release_id, join_relation, artist1, artist2)
												VALUES(%s,%s,%s,%s);"""
				artistIdx = release.artists.index(aj.artist1) + 1
				#The last join relation is not between artists but instead
				#something like "Bob & Alice 'PRESENTS' - Cryptographic Tunes":
				if artistIdx >= len(release.artists):
					values = (release.id, aj.join_relation, '', '')  # join relation is between all artists and the album
				else:
					values = (release.id, aj.join_relation, aj.artist1, release.artists[artistIdx])
				self.execute(query, values)
		else:
			if len(release.artists) == 0:  # use anv if no artist name
				self.execute("INSERT INTO releases_artists(release_id, artist_name) VALUES(%s,%s);",
						(release.id, release.anv))
			else:
				self.execute("INSERT INTO releases_artists(release_id, artist_name) VALUES(%s,%s);",
						(release.id, release.artists[0]))

		for extr in release.extraartists:
			self.execute("INSERT INTO releases_extraartists(release_id, artist_name, roles) VALUES(%s,%s,%s);",
						(release.id, extr.name, extr.roles))

		for trk in release.tracklist:
			trackid = str(uuid.uuid4())
			self.execute("INSERT INTO track(release_id, title, duration, position, track_id) VALUES(%s,%s,%s,%s,%s);",
					(release.id, trk.title, trk.duration, trk.position, trackid))
			for artist in trk.artists:
				query = "INSERT INTO tracks_artists(track_id, artist_name) VALUES(%s,%s);"
				self.execute(query, (trackid, artist))
			for aj in trk.artistJoins:
				query = """INSERT INTO tracks_artists_joins
											(track_id, join_relation, artist1, artist2)
											VALUES(%s,%s,%s,%s);"""
				artistIdx = trk.artists.index(aj.artist1) + 1
				if artistIdx >= len(trk.artists):
					values = (trackid, aj.join_relation, '', '')  # join relation is between all artists and the track
				else:
					values = (trackid, aj.join_relation, aj.artist1, trk.artists[artistIdx])
				self.execute(query, values)

				#Insert Extraartists for track
				for extr in trk.extraartists:
					#print extr.name
					#print extr.roles
					self.execute("INSERT INTO tracks_extraartists(track_id, artist_name) VALUES(%s,%s);", (trackid, extr.name))
					for role in extr.roles:
						if type(role).__name__ == 'tuple':
							#print trackid
							#print extr.name
							#print role[0]
							#print role[1]
							self.execute("INSERT INTO tracks_extraartists_roles(track_id, artist_name, role_name, role_details) VALUES(%s,%s,%s,%s);", (trackid, extr.name, role[0], role[1]))
						else:
							self.execute("INSERT INTO tracks_extraartists_roles(track_id, artist_name, role_name) VALUES(%s,%s,%s);", (trackid, extr.name, role))
		#'''

class PostgresConsoleDumper(PostgresExporter):

	def __init__(self, connection_string):
		super(PostgresConsoleDumper, self).__init__(connection_string)
		self.q = lambda x : "'%s'" % x.replace("'", "\\'")

	def connect(self, connection_string):
		pass

	def qs(self, what):
		ret = []
		for w in what:
			if type(w) == list:
				ret.append(self.qs(w))
			else:
				#print "q(%s)==%s" % (w, self.q(w))
				ret.append(self.q(w))

		return ret

	def execute(self, query, params):
		qparams = self.qs(params)
		print(query % tuple(qparams))
	
	def finish(self):
		pass
