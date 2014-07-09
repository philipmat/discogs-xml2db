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

import xml.sax.handler
import xml.sax
import sys
import model
import re

releaseCounter = 0


class ReleaseHandler(xml.sax.handler.ContentHandler):
	def __init__(self, exporter, stop_after=0, ignore_missing_tags=False):
		self.knownTags = (
							'id',
							'identifiers',
							'identifier',
							'videos',
							'video',
							'companies',
							'company',
							'catno',
							'entity_type',
							'entity_type_name',
							'resource_url',
							'anv',
							'artist',
							'artists',
							'country',
							'data_quality',
							'description',
							'descriptions',
							'duration',
							'extraartists',
							'format',
							'formats',
							'genre',
							'genres',
							#'indentifiers', 'identifier',
							'image',
							'images',
							'join',
							'label',
							'labels',
							'master_id',
							'name',
							'notes',
							'position',
							'release',
							'released',
							'releases',
							'role',
							'style',
							'styles',
							'sub_tracks',
							'title',
							'track',
							'tracklist',
							'tracks',
							'url',
							'urls',
							)
		self.release = None
		self.buffer = ''
		self.unknown_tags = []
		self.exporter = exporter
		self.stop_after = stop_after
		self.ignore_missing_tags = ignore_missing_tags
		self.stack = []

	def startElement(self, name, attrs):
		if not name in self.knownTags:
			if not self.ignore_missing_tags:
				print "Error: Unknown Release element '%s'." % name
				sys.exit()
			elif not name in self.unknown_tags:
				self.unknown_tags.append(name)
		self.stack.append(name)

		if name == 'release':
			self.release = model.Release()
			self.release.id = int(attrs['id'])
			self.release.status = attrs['status']

		elif name == 'track' and self.stack[-2] == 'tracklist':
			self.release.tracklist.append(model.Track())

		elif name == "image":
			img = model.ImageInfo()
			img.height = attrs["height"]
			img.imageType = attrs["type"]
			img.uri = attrs["uri"]
			img.uri150 = attrs["uri150"]
			img.width = attrs["width"]
			self.release.images.append(img)
			if len(attrs) != 5:
				print "ATTR ERROR"
				print attrs
				sys.exit()

		elif name == 'format':
			fmt = model.Format()
			fmt.name = attrs['name']
			fmt.qty = attrs['qty']
			self.release.formats.append(fmt)

		elif name == 'label':
			lbl = model.ReleaseLabel()
			lbl.name = attrs['name']
			lbl.catno = attrs['catno']
			self.release.labels.append(lbl)

		# Barcode
		elif name == 'identifier' and attrs['type'] == 'Barcode':
			self.release.barcode = attrs['value']


	def characters(self, data):
		self.buffer += data

	def endElement(self, name):
		self.buffer = self.buffer.strip()

		# Track title
		if name == 'title' and self.stack[-2] == 'track' and 'sub_track' not in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].title = self.buffer

		# Release title
		if name == 'title' and self.stack[-2] == 'release':
			if len(self.buffer) != 0:
				self.release.title = self.buffer

		# Release Country
		elif name == 'country':
			if len(self.buffer) != 0:
				self.release.country = self.buffer

		# Release Date
		elif name == 'released':
			if len(self.buffer) != 0:
				self.release.released = self.buffer

		# Release Notes
		elif name == 'notes':
			if len(self.buffer) != 0:
				self.release.notes = self.buffer

		# Release Genre	
		elif name == 'genre':
			if len(self.buffer) != 0:
				self.release.genres.append(self.buffer)

		# Release Style
		elif name == 'style':
			if len(self.buffer) != 0:
				self.release.styles.append(self.buffer)

		# Release Format Description
		elif name == 'description' and 'formats' in self.stack:
			if len(self.buffer) != 0:
				self.release.formats[-1].descriptions.append(self.buffer)

		# Release Quality
		elif name == 'data_quality':
			if len(self.buffer) != 0:
				self.release.data_quality = self.buffer

		# Track extra artist id
		elif name == 'id' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
		 		teaj = model.Extraartist()
				teaj.artist_id = self.buffer
				self.release.tracklist[-1].extraartists.append(teaj)

		# Release extra artist id
		elif name == 'id' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				eaj = model.Extraartist()
				eaj.artist_id = self.buffer
				self.release.extraartists.append(eaj)

		# Track artist id
		elif name == 'id' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				taj = model.ArtistJoin()
				taj.artist_id = self.buffer
				self.release.tracklist[-1].artistJoins.append(taj)

		# Release artist id
		elif name == 'id' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				aj = model.ArtistJoin()
				aj.artist_id = self.buffer
				self.release.artistJoins.append(aj)

		# Track extra artist name
		elif name == 'name' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].extraartists[-1].artist_name = self.buffer

		# Release extra artist name
		elif name == 'name' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				self.release.extraartists[-1].artist_name = self.buffer

		# Track artist name
		elif name == 'name' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].artistJoins[-1].artist_name = self.buffer

		# Release artist name
		elif name == 'name' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				self.release.artistJoins[-1].artist_name = self.buffer

		# Track artist anv
		elif name == 'anv' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].artistJoins[-1].anv = self.buffer

		# Track extra artist anv
		elif name == 'anv' and 'artist' in self.stack and 'track' in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].extraartists[-1].anv = self.buffer


		# Release artist anv
		elif name == 'anv' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' not in self.stack:
			if len(self.buffer) != 0:
				self.release.artistJoins[-1].anv = self.buffer

		# Release extra artist anv
		elif name == 'anv' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				self.release.extraartists[-1].anv = self.buffer

		# Track artist join
		elif name == 'join' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack:
			if len(self.buffer) != 0:
				self.release.tracklist[-1].artistJoins[-1].join_relation = self.buffer

		# Release artist join
		elif name == 'join' and 'artist' in self.stack and 'track' not in self.stack:
			if len(self.buffer) != 0:
				self.release.artistJoins[-1].join_relation = self.buffer

		# Track extra artist role
		elif name == 'role' and 'artist' in self.stack and 'track' in self.stack and 'sub_track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				roles_list = re.findall('([^[,]+(?:\[[^]]+])?)+', self.buffer)  # thanks to jlatour
				for role in roles_list:
					role = role.strip()
					lIndex = role.find('[')
					if lIndex != -1:
						rIndex = role.find(']')
						description = role[lIndex + 1: rIndex]
						role = (role[:lIndex].strip(), description)
					self.release.tracklist[-1].extraartists[-1].roles.append(role)

		# Release extra artist role
		elif name == 'role' and 'artist' in self.stack and 'track' not in self.stack and 'extraartists' in self.stack:
			if len(self.buffer) != 0:
				roles_list = re.findall('([^[,]+(?:\[[^]]+])?)+', self.buffer)  # thanks to jlatour
				for role in roles_list:
					role = role.strip()
					lIndex = role.find('[')
					if lIndex != -1:
						rIndex = role.find(']')
						description = role[lIndex + 1: rIndex]
						role = (role[:lIndex].strip(), description)
					self.release.extraartists[-1].roles.append(role)

		# Track Duration	
		elif name == 'duration' and 'sub_track' not in self.stack:
			self.release.tracklist[-1].duration = self.buffer

		# Track Position
		elif name == 'position' and 'sub_track' not in self.stack:
			self.release.tracklist[-1].position = self.buffer

		# Release Master
		elif name == 'master_id':
			self.release.master_id = int(self.buffer)

		# End of Release
		elif name == 'release':
			# end of tag
			len_a = len(self.release.artistJoins)
			if len_a == 0:
				sys.stderr.writelines("Ignoring Release %s with no artist. Dictionary: %s\n" % (self.release.id, self.release.__dict__))
			else:
				global releaseCounter
				releaseCounter += 1
				self.exporter.storeRelease(self.release)

				releaseCounter += 1
				if self.stop_after > 0 and releaseCounter >= self.stop_after:
					self.endDocument()
					if self.ignore_missing_tags and len(self.unknown_tags) > 0:
						print 'Encountered some unknown Release tags: %s' % (self.unknown_tags)
					raise model.ParserStopError(releaseCounter)

		if self.stack[-1] == name:
			self.stack.pop()
		self.buffer = ''

	def endDocument(self):
		#print [genre for genre in genres]
		#print [style for style in styles]
		#print [format for format in formats]
		#print [dsc for dsc in descriptions]
		#print [j for j in joins]
		#print [(role, roles[role]) for role in roles]
		#print len(roles)
		self.exporter.finish()
