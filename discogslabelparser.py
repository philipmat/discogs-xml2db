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
import model
import sys
import os

labelCounter = 0


class LabelHandler(xml.sax.handler.ContentHandler):
	inElement = {
				'id': False,
				'label': False,
				'labels': False,
				'data_quality': False,
				'contactinfo': False,
				'image': False,
				'images': False,
				'name': False,
				'profile': False,
				'parentLabel': False,
				'sublabels': False,
				'urls': False,
				'url': False,
				}
	label = model.Label()
	buffer = ''
	unknown_tags = []

	def __init__(self, exporter, stop_after=0, ignore_missing_tags = False):
		self.exporter = exporter
		self.stop_after = stop_after
		self.ignore_missing_tags = ignore_missing_tags

	def startElement(self, name, attrs):
		if not name in self.inElement:
			if not self.ignore_missing_tags:
				print "Error: Unknown Label element '%s'." % name
				sys.exit()
			elif not name in self.unknown_tags:
				self.unknown_tags.append(name)
		self.inElement[name] = True
		if name == 'label':
			if not self.inElement['sublabels']:
				self.label = model.Label()
		elif name == "image":
			newImage = model.ImageInfo()
			newImage.height = attrs["height"]
			newImage.imageType = attrs["type"]
			newImage.uri = attrs["uri"]
			newImage.uri150 = attrs["uri150"]
			newImage.width = attrs["width"]
			self.label.images.append(newImage)
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
			self.label.id = int(self.buffer)
		if name == 'name':
			if len(self.buffer) != 0:
				self.label.name = self.buffer
		elif name == 'contactinfo':
			if len(self.buffer) != 0:
				self.label.contactinfo = self.buffer
		elif name == 'data_quality':
			if len(self.buffer) != 0:
				self.label.data_quality = self.buffer
		elif name == 'profile':
			if len(self.buffer) != 0:
				self.label.profile = self.buffer
		elif name == 'url':
			if len(self.buffer) != 0:
				self.label.urls.append(self.buffer)
		elif name == 'parentLabel':
			if len(self.buffer) != 0:
				self.label.parentLabel = self.buffer
		elif name == "label":
			if self.inElement['sublabels']:
				if len(self.buffer) != 0:
					self.label.sublabels.append(self.buffer)
			else:
				self.exporter.storeLabel(self.label)

				global labelCounter
				labelCounter += 1
				if self.stop_after > 0 and labelCounter >= self.stop_after:
					self.endDocument()
					if self.ignore_missing_tags and len(self.unknown_tags) > 0:
						print 'Encountered some unknown Label tags: %s' % (self.unknown_tags)
					raise model.ParserStopError(labelCounter)

		self.inElement[name] = False
		self.buffer = ''

#labels = {}
#labelCounter = 0
