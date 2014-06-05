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
		elif name == 'track':
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
			#global formats
			#if not formats.has_key(attrs["name"]):
			#  formats[attrs["name"]] = True
		elif name == 'label':
			lbl = model.ReleaseLabel()
			lbl.name = attrs['name']
			lbl.catno = attrs['catno']
			self.release.labels.append(lbl)

	def characters(self, data):
		self.buffer += data

	def endElement(self, name):
		self.buffer = self.buffer.strip()
		if name == 'title':
			if len(self.buffer) != 0:
				# titles we know of can appear in tracks or releases
				# print "t=%s,k=%s" % (self.buffer, self.stack)
				if self.stack[-2] == 'track':
					self.release.tracklist[-1].title = self.buffer
				elif self.stack[-2] == 'release':
					self.release.title = self.buffer
		elif name == 'country':
			if len(self.buffer) != 0:
				self.release.country = self.buffer
				#global countries
				#if not countries.has_key(self.buffer):
				#  countries[self.buffer] = True
		elif name == 'anv':
			if len(self.buffer) != 0:
				if self.stack[-3] == 'artists' and self.stack[-4] == 'release':
					self.release.anv = self.buffer
				# TODO: support anv on tracks
		elif name == 'released':
			if len(self.buffer) != 0:
				self.release.released = self.buffer
		elif name == 'notes':
			if len(self.buffer) != 0:
				self.release.notes = self.buffer
		elif name == 'genre':
			if len(self.buffer) != 0:
				self.release.genres.append(self.buffer)
		elif name == 'style':
			if len(self.buffer) != 0:
				self.release.styles.append(self.buffer)
				#global styles
				#if not styles.has_key(self.buffer):
				#  styles[self.buffer] = Style(self.buffer)
		elif name == 'description':
			if len(self.buffer) != 0:
				if 'formats' in self.stack:
					self.release.formats[-1].descriptions.append(self.buffer)
		elif name == 'data_quality':
			if len(self.buffer) != 0:
				self.release.data_quality = self.buffer
		elif name == 'name':
			if len(self.buffer) != 0:
				if 'extraartists' in self.stack:
					if 'track' in self.stack:  # extraartist for track
						track = self.release.tracklist[-1]
						extr = model.Extraartist()
						extr.name = self.buffer
						track.extraartists.append(extr)
					else:  # extraartists for release
						extr = model.Extraartist()
						extr.name = self.buffer
						self.release.extraartists.append(extr)
				elif 'track' in self.stack and 'extraartists' not in self.stack:
					self.release.tracklist[-1].artists.append(self.buffer)
				else:  # release artist
					self.release.artists.append(self.buffer)
		elif name == 'join':
			if len(self.buffer) != 0:
				if 'track' in self.stack:  # extraartist
					track = self.release.tracklist[-1]
					aj = model.ArtistJoin()
					#print "ext: " + str(track.extraartists)
					#print "title: " + str(track.title)
					#print "artists: " + str(track.artists)
					if len(track.artists) > 0:  # fix for bug with release 2033428, track 3
						aj.artist1 = track.artists[-1]
						aj.join_relation = self.buffer
						track.artistJoins.append(aj)
				else:  # main release artist
					aj = model.ArtistJoin()
					if len(self.release.artists) > 0:
						aj.artist1 = self.release.artists[-1]
					else:
						aj.artist1 = self.release.anv
						self.release.artists.append(self.release.anv)
					aj.join_relation = self.buffer
					self.release.artistJoins.append(aj)
		elif name == 'role':
			if len(self.buffer) != 0:
				#print "ROLE PRE" + str(self.buffer)
				roles_list = re.findall('([^[,]+(?:\[[^]]+])?)+', self.buffer)  # thanks to jlatour
				#print "ROLE POST" + str(self.buffer)
				for role in roles_list:
					role = role.strip()
					lIndex = role.find('[')
					if lIndex != -1:
						rIndex = role.find(']')
						description = role[lIndex + 1: rIndex]
						role = (role[:lIndex].strip(), description)
					if 'track' in self.stack:
						idx = len(self.release.tracklist) - 1
						track = self.release.tracklist[idx]
						if len(track.extraartists) != 0:
							trackExtraartist = track.extraartists[-1]
							trackExtraartist.roles.append(role)
					else:
						self.release.extraartists[-1].roles.append(role)
		elif name == 'duration':
			self.release.tracklist[-1].duration = self.buffer
		elif name == 'position':
			self.release.tracklist[-1].position = self.buffer
		elif name == 'master_id':
			self.release.master_id = int(self.buffer)
		elif name == 'release':
			# end of tag
			len_a = len(self.release.artists)
			if len_a == 0:
				sys.stderr.writelines("Ignoring Release %s with no artist. Dictionary: %s\n" % (self.release.id, self.release.__dict__))
			else:
				if len(self.release.artists) == 1:
					self.release.artist = self.release.artists[0]
				else:
					for j in self.release.artistJoins:
						self.release.artist += '%s %s ' % (j.artist1, j.join_relation)
					self.release.artist += self.release.artists[-1]

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
