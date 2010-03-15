#!/usr/bin/python
# -*- coding: utf-8 -*-
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

import xml.sax.handler
import xml.sax
import os
import sys
import codecs
import model
from postgresexporter import PostgresExporter
import MySQLdb#used to escape strings
import psyco 
psyco.full()

#artists = {}
artistCounter = 0

class ArtistHandler(xml.sax.handler.ContentHandler):
  inElement = {'artists':False,'artist':False,'name':False,'realname':False,'image':False,'images':False,'urls':False,'url':False,'namevariations':False,'aliases':False,'profile':False,'groups':False,'members':False}
  artist = None
  buffer = ''

  def __init__(self):
    self.artist = model.Artist()
    self.psqlexporter = PostgresExporter()
    #self.inElement = 
    #self.element = {}
 
  def startElement(self, name, attrs):
    if not self.inElement.has_key(name):
      print "ERROR, UNKOWN ELEMENT!!!"
      print name
      sys.exit()
    self.inElement[name] = True

    if name == "artist":
       self.artist = model.Artist()
    elif name == "image":
      self.artist.images.append(model.ImageInfo())
      self.artist.images[len(self.artist.images)-1].height =  attrs["height"]
      self.artist.images[len(self.artist.images)-1].imageType =  attrs["type"]
      self.artist.images[len(self.artist.images)-1].uri =  attrs["uri"]
      self.artist.images[len(self.artist.images)-1].uri150 =  attrs["uri150"]
      self.artist.images[len(self.artist.images)-1].width =  attrs["width"]
      if len(attrs) != 5:
         print "ATTR ERROR"
         print attrs
         sys.exit()
 
  def characters(self, data):
    self.buffer += data
 
  def endDocument(self):
    self.psqlexporter.finish()

  def endElement(self, name):
    self.buffer = self.buffer.strip()
    self.buffer = MySQLdb.escape_string(self.buffer)
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
       #global artists
       #artists[self.artist.name] = self.artist

       global artistCounter
       artistCounter += 1

       values = []
       values.append(self.artist.name)
       columns = "name"

       if len(self.artist.realname) != 0:
         values.append(self.artist.realname)
         columns += ",realname"
       if len(self.artist.profile) != 0:
         values.append(self.artist.profile)
         columns += ",profile"
       if len(self.artist.namevariations) != 0:
         values.append(self.artist.namevariations)
         columns += ",namevariations"
       if len(self.artist.urls) != 0:
         values.append(self.artist.urls)
         columns += ",urls"
       if len(self.artist.aliases) != 0:
         values.append(self.artist.aliases)
         columns += ",aliases"
       if len(self.artist.groups) != 0:
         values.append(self.artist.groups)
         columns += ",groups"
       if len(self.artist.members) != 0:
         values.append(self.artist.members)
         columns += ",members"

       self.psqlexporter.storeArtist(columns, values, self.artist)

       print artistCounter
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
                  print "urls: " + str(artists[artist].urls)
                  print "members: " + str(artists[artist].members)
                  print "groups: " + str(artists[artist].groups)
                  if len(artists[artist].members) == 0:
                     print "Not a group"
		  for img in artists[artist].images:
		     print "type: " + img.imageType + "size: " + str(img.height) + "x" + str(img.width) + " uri: " + img.uri + " uri150: " + img.uri150
	       os._exit(0)
       #'''



