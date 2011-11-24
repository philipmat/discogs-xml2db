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
	inElement = {'artists':False,'artist':False,'name':False,'realname':False,'image':False,'images':False,'urls':False,'url':False,'namevariations':False,'aliases':False,'profile':False,'groups':False,'members':False}
	artist = None
	buffer = ''
	unknown_tags = []

	def __init__(self, exporter, stop_after=0, ignore_missing_tags = False):
		self.artist = model.Artist()
		self.exporter = exporter
		#self.inElement = 
		#self.element = {}
		#global options
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
				'''
				if self.buffer.find('wikipedia') != -1:
				self.artist.urls['wikipedia'] = self.buffer
				elif self.buffer.find('myspace') != -1:
				self.artist.urls['myspace'] = self.buffer
				else: 
				self.artist.urls['other'].append(self.buffer)
				'''
		elif name == "artist":

			self.exporter.storeArtist(self.artist)
			global artistCounter
			artistCounter += 1
			if self.stop_after > 0 and artistCounter >= self.stop_after:
				self.endDocument()
				if self.ignore_missing_tags and len(self.unknown_tags) > 0:
					print 'Encountered some unknown Artist tags: %s' % (self.unknown_tags)
				raise model.ParserStopError(artistCounter)

		self.buffer = ''
		self.inElement[name] = False

		'''
		if len(artists) > 100:
				for artist in artists:
						#print "aRIST+" + artists[artist]
				print "name: " + artists[artist].name
				print "realname: " + artists[artist].realname
				print "namevariations: " + str(artists[artist].namevariations)
				print "aliases: " + str(artists[artist].aliases)
						print "profile: " + artists[artist].profile
						rint "urls: " + str(artists[artist].urls)
						print "members: " + str(artists[artist].members)
						print "groups: " + str(artists[artist].groups)
						if len(artists[artist].members) == 0:
							print "Not a group"
				for img in artists[artist].images:
					print "type: " + img.imageType + "size: " + str(img.height) + "x" + str(img.width) + " uri: " + img.uri + " uri150: " + img.uri150
				os._exit(0)
			#'''
