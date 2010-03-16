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

import psycopg2
import uuid
import sys

host = 'localhost'
user = 'discogs'
dbname = 'discogs'

dbparams = "dbname='%s' user='%s' host='%s'" % (dbname, user, host)

class PostgresExporter:
    def __init__(self): 
      self.formatNames = {}
      self.imgUris = {}
      try:
          self.conn = psycopg2.connect(dbparams)
          self.cur = self.conn.cursor()
          self.conn.set_isolation_level(0)
      except psycopg2.Error, e:
          print "%s" % (e.args)
          sys.exit()

    def finish(self):
        self.conn.commit()
        self.cur.close()

    def storeLabel(self, columns, values, label):
         escapeStrings = ''
         for counter in xrange(1,len(columns.split(","))):
           escapeStrings = escapeStrings + ",%s"
         escapeStrings = '(%s'+escapeStrings+')'
         #print values
         query = "INSERT INTO label("+columns+") VALUES"+escapeStrings+";"
         #print query
         try:
           self.cur.execute(query,values)
         except psycopg2.Error, e:
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
             if not self.imgUris.has_key(img.uri):
               imgQuery = "INSERT INTO image("+imgCols+") VALUES(%s,%s,%s,%s,%s);"
               self.cur.execute(imgQuery,imgValues)
               self.imgUris[img.uri] = True
             self.cur.execute("INSERT INTO labels_images(image_uri, label_name) VALUES(%s,%s);",(img.uri, label.name))

    def storeArtist(self, columns, values, artist):
       escapeStrings = ''
       for counter in xrange(1,len(columns.split(","))):
         escapeStrings = escapeStrings + ",%s"
       escapeStrings = '(%s'+escapeStrings+')'
       #print values
       query = "INSERT INTO artist("+columns+") VALUES"+escapeStrings+";"
       #print query
       try:
         self.cur.execute(query,values)
       except psycopg2.Error, e:
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
           if not self.imgUris.has_key(img.uri):
             imgQuery = "INSERT INTO image("+imgCols+") VALUES(%s,%s,%s,%s,%s);"
             self.cur.execute(imgQuery,imgValues)
             self.imgUris[img.uri] = True
           self.cur.execute("INSERT INTO artists_images(image_uri, artist_name) VALUES(%s,%s);",(img.uri, artist.name))

    def storeRelease(self, columns, values, release):
         #'''INSERT INTO DATABASE
         escapeStrings = ''
         for counter in xrange(1,len(columns.split(","))):
           escapeStrings = escapeStrings + ",%s"
         escapeStrings = '(%s'+escapeStrings+')'
         #print values
         query = "INSERT INTO release("+columns+") VALUES"+escapeStrings+";"
         #print query
         try:
           self.cur.execute(query,values)
         except psycopg2.Error, e:
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
             if not self.imgUris.has_key(img.uri):
               self.cur.execute("SELECT uri FROM image WHERE uri='"+img.uri+"';")
               if len(self.cur.fetchall()) == 0:
                 imgQuery = "INSERT INTO image("+imgCols+") VALUES(%s,%s,%s,%s,%s);"
                 self.cur.execute(imgQuery,imgValues)
               self.imgUris[img.uri] = True
             self.cur.execute("INSERT INTO releases_images(image_uri, discogs_id) VALUES(%s,%s);",
                        (img.uri, release.discogs_id))
         for fmt in release.formats:
           if len(release.formats) != 0:
             if not self.formatNames.has_key(fmt.name):
                self.formatNames[fmt.name] = True
                try:
                  self.cur.execute("INSERT INTO format(name) VALUES('"+fmt.name+"');")
                except psycopg2.Error, e:
                  print "%s" % (e.args)
                  return   
             query = "INSERT INTO releases_formats(discogs_id, format_name, qty, descriptions) VALUES(%s,%s,%s,%s);"
             self.cur.execute(query,(release.discogs_id, fmt.name, fmt.qty, fmt.descriptions))
         labelQuery = "INSERT INTO releases_labels(discogs_id, label, catno) VALUES(%s,%s,%s);"
         for lbl in release.labels:         
           self.cur.execute(labelQuery,(release.discogs_id, lbl.name, lbl.catno))

         if len(release.artists) > 1:
           for artist in release.artists:
             query = "INSERT INTO releases_artists(discogs_id, artist_name) VALUES(%s,%s);"
             self.cur.execute(query,(release.discogs_id, artist))
           for aj in release.artistJoins:
             query = """INSERT INTO releases_artists_joins
                                              (discogs_id, join_relation, artist1, artist2) 
                                               VALUES(%s,%s,%s,%s);"""
             artistIdx = release.artists.index(aj.artist1)+1
             #The last join relation is not between artists but instead 
             #something like "Bob & Alice 'PRESENTS' - Cryptographic Tunes":
             if  artistIdx >= len(release.artists): 
               values = (release.discogs_id, aj.join_relation, '', '')#join relation is between all artists and the album
             else:
               values = (release.discogs_id, aj.join_relation, aj.artist1, release.artists[artistIdx])
             self.cur.execute(query, values)
         else:
           if len(release.artists) == 0: # use anv if no artist name
             self.cur.execute("INSERT INTO releases_artists(discogs_id, artist_name) VALUES(%s,%s);",
                        (release.discogs_id, release.anv))
           else:
             self.cur.execute("INSERT INTO releases_artists(discogs_id, artist_name) VALUES(%s,%s);",
                        (release.discogs_id, release.artists[0]))

         for extr in release.extraartists:
           self.cur.execute("INSERT INTO releases_extraartists(discogs_id, artist_name, roles) VALUES(%s,%s,%s);",
                       (release.discogs_id, extr.name, extr.roles))

         for trk in release.tracklist:
           trackid = str(uuid.uuid4())
           self.cur.execute("INSERT INTO track(discogs_id, title, duration, position, track_id) VALUES(%s,%s,%s,%s,%s);",
                      (release.discogs_id, trk.title, trk.duration, trk.position, trackid))
           for artist in trk.artists:
             query = "INSERT INTO tracks_artists(track_id, artist_name) VALUES(%s,%s);"
             self.cur.execute(query,(trackid, artist))
           for aj in trk.artistJoins:
             query = """INSERT INTO tracks_artists_joins
                                              (track_id, join_relation, artist1, artist2) 
                                               VALUES(%s,%s,%s,%s);"""
             artistIdx = trk.artists.index(aj.artist1)+1
             if artistIdx >= len(trk.artists): 
               values = (trackid, aj.join_relation, '', '')#join relation is between all artists and the track
             else:
               values = (trackid, aj.join_relation, aj.artist1, trk.artists[artistIdx])
             self.cur.execute(query, values)

           #Insert Extraartists for track
           for extr in trk.extraartists:
             #print extr.name
             #print extr.roles
             self.cur.execute("INSERT INTO tracks_extraartists(track_id, artist_name) VALUES(%s,%s);", (trackid, extr.name))
             '''Commented out until bug fixed with roleparsing Bleh [asdf,asdf,asdf], hurra[heo]
             for role in extr.roles:
               if type(role).__name__=='tuple':
                 print trackid
                 print extr.name
                 print role[0]
                 print role[1]
                 self.cur.execute("INSERT INTO tracks_extraartists_roles(track_id, artist_name, role_name, role_details) VALUES(%s,%s,%s,%s);", (trackid, extr.name, role[0], role[1]))
               else:
                 self.cur.execute("INSERT INTO tracks_extraartists_roles(track_id, artist_name, role_name) VALUES(%s,%s,%s);", (trackid, extr.name, role))
             #'''
         #'''
