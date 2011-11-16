import pymongo
import urlparse
from jsonexporter import jsonizer as _jsonizer
import json

def jsonizer(obj):
	return _jsonizer(obj, specify_object_type = False)

class MongoDbExporter(object):
	
	def __init__(self, mongo_uri):
		'''mongo_uri: mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]'''
		self.server = mongo_uri
		self.connect(mongo_uri)

	def connect(self, mongo_uri):
		db_name = None
		u = urlparse.urlparse(mongo_uri)
		if '?' in u.path and u.query == '':
			#url didn't parse it properly u.path is '/dbname?options
			db_name = u.path.split('?')[0]
		else:
			db_name = u.path
		if db_name.startswith('/'):
			db_name = db_name[1:]
		print 'Connecting to db %s on %s.' % (db_name, mongo_uri)
		mongo = pymongo.Connection(mongo_uri)
		self.db = mongo[db_name]

	def execute(self, collection, what):
		# have to convert it to json and back because 
		# on simple objects couchdb-python throws:
		# TypeError: argument of type 'instance' is not iterable
		# and on dicts:
		# AttributeError: 'dict' object has no attribute 'read'
		doc = json.loads(json.dumps(what, default=jsonizer))
		self.db[collection].update({'id' : what.id}, doc, upsert = True)
	

	def finish(self, completely_done = False):
		collections = self.db.collection_names()
		if 'artists' in collections:
			self.db.artists.ensure_index('id', background = True)
		if 'labels' in collections:
			self.db.labels.ensure_index('id', background = True)
		if 'releases' in collections:
			self.db.releases.ensure_index('artist', background = True)
			self.db.releases.ensure_index('format.name', background = True)
			self.db.releases.ensure_index('title', background = True)
		self.db.connection.disconnect()


	def storeLabel(self, label):
		label.id = label.name
		self.execute('labels', label)
	
	def storeArtist(self, artist):
		artist.id = artist.name
		self.execute('artists', artist)
	
	def storeRelease(self, release):
		release.id = release.discogs_id
		self.execute('releases', release)

