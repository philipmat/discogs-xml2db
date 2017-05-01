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

	def __init__(self, exporter, stop_after=0, ignore_missing_tags=False):
		self.exporter = exporter
		self.stop_after = stop_after
		self.ignore_missing_tags = ignore_missing_tags
		self.labelCounter = 0

	def startElement(self, name, attrs):
		if name not in self.inElement:
			if not self.ignore_missing_tags:
				print("Error: Unknown Label element '%s'." % name)
				sys.exit()
			elif name not in self.unknown_tags:
				self.unknown_tags.append(name)
		self.inElement[name] = True
		if name == 'label':
			if not self.inElement['sublabels']:
				self.label = model.Label()
		elif name == "image":
			newImage = model.ImageInfo()
			for f in ("height", "type", "uri", "uri150", "width"):
				setattr(newImage, f, attrs[f])
			self.label.images.append(newImage)
			if len(attrs) != 5:
				print("ATTR ERROR")
				print(attrs)
				sys.exit()

	def characters(self, data):
		self.buffer += data

	def endDocument(self):
		self.exporter.finish()

	def endElement(self, name):
		self.buffer = self.buffer.strip()
		if name == 'id':
			self.label.id = int(self.buffer)
		elif name in ('name', 'contactinfo', 'data_quality', 'profile', 'parentLabel'):
			if len(self.buffer) != 0:
				setattr(self.label, name, self.buffer)
		elif name == 'url':
			if len(self.buffer) != 0:
				self.label.urls.append(self.buffer)
		elif name == "label":
			if self.inElement['sublabels']:
				if len(self.buffer) != 0:
					self.label.sublabels.append(self.buffer)
			else:
				self.exporter.storeLabel(self.label)

				self.labelCounter += 1
				if self.stop_after > 0 and self.labelCounter >= self.stop_after:
					self.endDocument()
					if self.ignore_missing_tags and len(self.unknown_tags) > 0:
						print('Encountered some unknown Label tags: %s' % (self.unknown_tags))
					raise model.ParserStopError(self.labelCounter)

		self.inElement[name] = False
		self.buffer = ''
