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
import pprint
import MySQLdb
import model
import sys
import os
from postgresexporter import PostgresExporter

class LabelHandler(xml.sax.handler.ContentHandler):
  inElement = {'name':False,'label':False,'labels':False,'contactinfo':False,'image':False,'images':False,'urls':False,'url':False,'profile':False,'parentLabel':False,'sublabels':False}
  label = model.Label()
  buffer = ''

  def __init__(self):
    self.psqlexporter = PostgresExporter()

  def startElement(self, name, attrs):
    if not self.inElement.has_key(name):
      print "ERROR, UNKOWN ELEMENT!!!"
      print name
      sys.exit()
    self.inElement[name] = True
    if name == 'label':
      if not self.inElement['sublabels']:
        self.label = model.Label()
    elif name == "image":
      self.label.images.append(model.ImageInfo())
      self.label.images[len(self.label.images)-1].height =  attrs["height"]
      self.label.images[len(self.label.images)-1].imageType =  attrs["type"]
      self.label.images[len(self.label.images)-1].uri =  attrs["uri"]
      self.label.images[len(self.label.images)-1].uri150 =  attrs["uri150"]
      self.label.images[len(self.label.images)-1].width =  attrs["width"]
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
         self.label.name = self.buffer
    elif name == 'contactinfo':
      if len(self.buffer) != 0:
          self.label.contactinfo = self.buffer
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
         #global labels
         global labelCounter
         labelCounter += 1
         print labelCounter
         #labels[self.label.name] = self.label    
         
         values = []
         values.append(self.label.name)
         columns = "name"

         if len(self.label.contactinfo) != 0:
           values.append(self.label.contactinfo)
           columns += ",contactinfo"
         if len(self.label.profile) != 0:
           values.append(self.label.profile)
           columns += ",profile"
         if len(self.label.parentLabel) != 0:
           values.append(self.label.parentLabel)
           columns += ",parent_label"
         if len(self.label.urls) != 0:
           values.append(self.label.urls)
           columns += ",urls"
         if len(self.label.sublabels) != 0:
           values.append(self.label.sublabels)
           columns += ",sublabels"

         self.psqlexporter.storeLabel(columns, values, self.label)

         '''
         if len(labels) > 100:
           for label in labels:
             print "name: " + labels[label].name
             print "contactinfo: " + labels[label].contactinfo
             print "profile: " + labels[label].profile
             print "urls: " + str(labels[label].urls)
             print "parentLabel: " + str(labels[label].parentLabel)
             print "subLabels: " + str(labels[label].sublabels)
             #for img in labels[label].images:
             #   print "type: " + img.imageType + "size: " + str(img.height) + "x" + str(img.width) + " uri: " + img.uri + " uri150: " + img.uri150
           os._exit(0)
         #'''
    self.inElement[name] = False
    self.buffer = ''

#labels = {}
labelCounter = 0


reload(sys)
sys.setdefaultencoding('utf-8')
