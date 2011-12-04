import couchdb
import urlparse
from jsonexporter import jsonizer
import json


class CouchDbExporter(object):
	
	def __init__(self, server_url):
		self.server = server_url
		self.connect(server_url)

	def connect(self, server_url):
		u = urlparse.urlparse(server_url)
		db_name = u.path.split('/')[1]
		server = "%s://%s" % (u.scheme, u.netloc)
		print 'Connecting to %s and database %s.' % (server, db_name)
		couch = couchdb.Server(server)
		self.db = couch[db_name]

	def execute(self, what):
		# have to convert it to json and back because 
		# on simple objects couchdb-python throws:
		# TypeError: argument of type 'instance' is not iterable
		# and on dicts:
		# AttributeError: 'dict' object has no attribute 'read'
		doc = json.loads(json.dumps(what, default=jsonizer))
		self.db.save(doc)
	

	def finish(self, completely_done = False):
		pass

	def storeLabel(self, label):
		self.execute(label)
	
	def storeArtist(self, artist):
		self.execute(artist)
	
	def storeRelease(self, release):
		self.execute(release)

