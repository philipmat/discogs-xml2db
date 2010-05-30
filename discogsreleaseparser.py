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
import MySQLdb
import sys
import model
import psycopg2
from postgresexporter import PostgresExporter
import psyco 
psyco.full()

class ReleaseHandler(xml.sax.handler.ContentHandler):
  def __init__(self):
    self.inElement = {'releases':False,'release':False,'name':False,'descriptions':False,
                      'description':False,'genres':False,'genre':False,'styles':False,
                      'style':False,'label':False,'labels':False,'title':False,
                      'image':False,'images':False,'urls':False,'url':False,
                      'artists':False,'artist':False,'formats':False,'format':False,
                      'country':False,'released':False,'notes':False,'tracklist':False,
                      'track':False,'position':False,'duration':False,'join':False,
                      'anv':False,'extraartists':False,'role':False}
    self.release = None
    self.buffer = ''
    self.psqlexporter = PostgresExporter()
 
  def startElement(self, name, attrs):
    if not self.inElement.has_key(name):
      print "ERROR, UNKOWN ELEMENT!!!"
      print name
      sys.exit()
    self.inElement[name] = True
    if name == 'release':
      self.release = model.Release()
      self.release.discogs_id = attrs['id']
      self.release.status = attrs['status']
    elif name == 'track':
      self.release.tracklist.append(model.Track())
    elif name == "image":
      img = model.ImageInfo()
      img.height =  attrs["height"]
      img.imageType =  attrs["type"]
      img.uri =  attrs["uri"]
      img.uri150 =  attrs["uri150"]
      img.width =  attrs["width"]
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
    self.buffer = MySQLdb.escape_string(self.buffer)
    if name == 'title':
      if len(self.buffer) != 0:
        if self.inElement['track']:
            self.release.tracklist[len(self.release.tracklist)-1].title = self.buffer
        else:
          self.release.title = self.buffer
    elif name == 'country':
      if len(self.buffer) != 0:
        self.release.country = self.buffer
        #global countries
        #if not countries.has_key(self.buffer):
        #  countries[self.buffer] = True
    elif name == 'anv':
      if len(self.buffer) != 0:
        if not self.inElement['track']:
          self.release.anv = self.buffer 
    elif name == 'released':
      if len(self.buffer) != 0:
        self.release.released = self.buffer 
    elif name == 'notes':
      if len(self.buffer) != 0:
        self.release.notes = self.buffer
    elif name == 'genre':
      if len(self.buffer) != 0:
        self.release.genres.append(self.buffer)
        #global genres
        #if not genres.has_key(self.buffer):
        #  genres[self.buffer] = Genre(self.buffer)
    elif name == 'style':
      if len(self.buffer) != 0:
        self.release.styles.append(self.buffer)
        #global styles
        #if not styles.has_key(self.buffer):
        #  styles[self.buffer] = Style(self.buffer)
    elif name == 'description':
      if len(self.buffer) != 0:
        self.release.formats[len(self.release.formats)-1].descriptions.append(self.buffer)
        #global descriptions
        #if not descriptions.has_key(self.buffer):
        #  descriptions[self.buffer] = True
    elif name == 'name':
      if len(self.buffer) != 0:
        if self.inElement['extraartists']:
          if self.inElement['track']:#extraartist for track
            track = self.release.tracklist[len(self.release.tracklist)-1]
            extr = model.Extraartist()
            extr.name = self.buffer
            track.extraartists.append(extr)
          else:#extraartists for release
            self.release.extraartists.append(Extraartist())
            self.release.extraartists[len(self.release.extraartists)-1].name = self.buffer
        elif self.inElement['track'] and not self.inElement['extraartists']:
          self.release.tracklist[len(self.release.tracklist)-1].artists.append(self.buffer)
        else: #release artist
          self.release.artists.append(self.buffer)
    elif name == 'join':
      if len(self.buffer) != 0:
        if self.inElement['track']:#extraartist
          track = self.release.tracklist[len(self.release.tracklist)-1]
          aj = model.ArtistJoin()
          print "ext: " + str(track.extraartists)
          print "title: " + str(track.title)
          print "artists: " + str(track.artists)
          if len(track.artists) > 0: #fix for bug with release 2033428, track 3
            aj.artist1 = track.artists[len(track.artists)-1]
            aj.join_relation = self.buffer
            track.artistJoins.append(aj)
        else:#main release artist
          aj = model.ArtistJoin()
          if len(self.release.artists) > 0:
            aj.artist1 = self.release.artists[-1]
          else:
            aj.artist1 = self.release.anv
          aj.join_relation = self.buffer
          self.release.artistJoins.append(aj)
        #global joins
        #if not joins.has_key(self.buffer):
        #  joins[self.buffer] = True
    elif name == 'role':
      if len(self.buffer) != 0:
        print "ROLE PRE" + str(self.buffer)
        roles_list = re.findall('([^[,]+(?:\[[^]]+])?)+', self.buffer) #thanks to jlatour
        print "ROLE POST" + str(self.buffer)
        for role in roles_list:
          role = role.strip()
          lIndex = role.find('[') 
          if lIndex != -1:
            rIndex = role.find(']')
            description = role[lIndex+1:rIndex]
            role = (role[:lIndex].strip(), description)
          if self.inElement['track']:
            idx = len(self.release.tracklist)-1
            track = self.release.tracklist[idx]
            if len(track.extraartists) != 0:
              trackExtraartist = track.extraartists[len(track.extraartists)-1]
              trackExtraartist.roles.append(role)
          else:
            self.release.extraartists[len(self.release.extraartists)-1].roles.append(role)
    elif name == 'duration':
      self.release.tracklist[len(self.release.tracklist)-1].duration = self.buffer
    elif name == 'position':
      self.release.tracklist[len(self.release.tracklist)-1].position = self.buffer
    elif name == 'release':
      global releaseCounter
      releaseCounter += 1
      #global releases
      #releases.append(self.release)
      #print releaseCounter
      print self.release.discogs_id
      #if releaseCounter > 1000:
      #  self.endDocument()
      #print self.release.title
      #'''PREPARE FOR DATABASE       
      values = []
      values.append(self.release.discogs_id)
      values.append(self.release.title)
      values.append(self.release.status)
      columns = "discogs_id, title, status"

      if len(self.release.country) != 0:
       values.append(self.release.country)
       columns += ",country"
      if len(self.release.released) != 0:
       values.append(self.release.released)
       columns += ",released"
      if len(self.release.notes) != 0:
       values.append(self.release.notes)
       columns += ",notes"
      if len(self.release.genres) != 0:
       values.append(self.release.genres)
       columns += ",genres"
      if len(self.release.styles) != 0:
       values.append(self.release.styles)
       columns += ",styles"
      #'''

      self.psqlexporter.storeRelease(columns, values, self.release)

      '''PRINT DATA
      if len(releases) > 50:
       for release in releases:
         print "------------------------------------------------"
         print "id, title, status: " + release.discogs_id + ", " + release.title + ", " + release.status
         print "country: " + release.country
         print "releasedate: " + release.released
         print "notes: " + release.notes
         print "genres: " + str(release.genres)
         print "styles: " + str(release.styles)
         sys.stdout.write("formats: ")
         for fmt in release.formats:
           print fmt.name + " " + fmt.qty + " " + str(fmt.descriptions)
         for img in release.images:
           print "type: " + img.imageType + "size: " + str(img.height) + "x" + str(img.width) + " uri: " + img.uri + " uri150: " + img.uri150
         sys.stdout.write("labels: ")
         for lbl in release.labels:
           print lbl.name + " " + lbl.catno
         print "artists: " + str(release.artists)
         print "artistJoins: " + str(release.artistJoins)
         for extr in release.extraartists:
           print "extraartist: " + extr.name + " " + str(extr.roles)              
         print "tracklist:"
         for trk in release.tracklist:
           print "track: " + trk.title + " " + trk.duration + " " +trk.position
           print "artists: " + str(trk.artists)
           print "artistJoins: " + str(trk.artistJoins)
           sys.stdout.write("extraartists:") 
           for extr in trk.extraartists:
             print "extraartist: " + extr.name + " " + str(extr.roles)              
       sys.exit()
      #'''
    self.inElement[name] = False
    self.buffer = ''

  def endDocument(self):
    #print [genre for genre in genres]
    #print [style for style in styles]
    #print [format for format in formats]
    #print [dsc for dsc in descriptions]
    #print [j for j in joins]
    #print [(role, roles[role]) for role in roles]
    #print len(roles)
    self.psqlexporter.finish()


#genres = {}
#styles = {}
#countries = {}
#formats = {}
#descriptions = {}
#releases = []
#joins = {}
#roles = {}
releaseCounter = 0

