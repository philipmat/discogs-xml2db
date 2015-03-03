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
import os
import sys
import model
#import psyco
#psyco.full()

artistCounter = 0


class ArtistHandler(xml.sax.handler.ContentHandler):
	inElement = {
				'artists': False,
				'artist': False,
				'aliases': False,
				'data_quality': False,
				'groups': False,
				'id': False,
				'image': False,
				'images': False,
				'members': False,
				'name': False,
				'namevariations': False,
				'profile': False,
				'realname': False,
				'urls': False,
				'url': False,
				}
	artist = None
	buffer = ''
	unknown_tags = []

	def __init__(self, exporter, stop_after=0, ignore_missing_tags=False):
		self.artist = model.Artist()
		self.exporter = exporter
		self.stop_after = stop_after
		self.ignore_missing_tags = ignore_missing_tags

	def startElement(self, name, attrs):
		if not name in self.inElement:
			if not self.ignore_missing_tags:
				print "Error: Unknown Artist element '%s'." % name
				sys.exit()
			elif not name in self.unknown_tags:
				self.unknown_tags.append(name)
		self.inElement[name] = True

		if name == "artist":
			self.artist = model.Artist()
		elif name == "image":
			image = model.ImageInfo()
			image.height = attrs["height"]
			image.imageType = attrs["type"]
			image.uri = attrs["uri"]
			image.uri150 = attrs["uri150"]
			image.width = attrs["width"]
			self.artist.images.append(image)
			if len(attrs) != 5:
				print "ATTR ERROR"
				print attrs
				sys.exit()

	def characters(self, data):
		self.buffer += data

	def endDocument(self):
		self.exporter.finish()

	def endElement(self, name):
		self.buffer = self.buffer.strip()
		if name == 'id':
                 	if not self.inElement['members']:
				self.artist.id = int(self.buffer)
		if name == 'name':
			if len(self.buffer) != 0:
				if self.inElement['namevariations']:
					self.artist.namevariations.append(self.buffer)
				elif self.inElement['aliases']:
					self.artist.aliases.append(self.buffer)
				elif self.inElement['groups']:
					self.artist.groups.append(self.buffer)
				elif self.inElement['members']:
					self.artist.members.append(self.buffer)
				else:
					self.artist.name = self.buffer
		elif name == 'realname':
			if len(self.buffer) != 0:
				self.artist.realname = self.buffer
		elif name == 'profile':
			if len(self.buffer) != 0:
				self.artist.profile = self.buffer
		elif name == 'url':
			if len(self.buffer) != 0:
				self.artist.urls.append(self.buffer)
		elif name == 'data_quality':
			if len(self.buffer) != 0:
				self.artist.data_quality = self.buffer
		elif name == "artist":

			if self.artist.name:
				self.exporter.storeArtist(self.artist)
				global artistCounter
				artistCounter += 1
				if self.stop_after > 0 and artistCounter >= self.stop_after:
					self.endDocument()
					if self.ignore_missing_tags and len(self.unknown_tags) > 0:
						print 'Encountered some unknown Artist tags: %s' % (self.unknown_tags)
					raise model.ParserStopError(artistCounter)
			else:
				sys.stderr.writelines("Ignoring Artist %s with no name. Dictionary: %s\n" % (self.artist.id, self.artist.__dict__))

		self.buffer = ''
		self.inElement[name] = False
