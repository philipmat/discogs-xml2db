from hashlib import md5
from jsonexporter import jsonizer as _jsonizer
import json
import os
import pymongo
import urlparse

def jsonizer(obj):
	return _jsonizer(obj, specify_object_type=False)


class _MongoImportFile(object):
	def __init__(self, name, path):
		self._path = os.path.abspath(os.path.expanduser(path))
		self._fname = os.path.join(self._path, name + '.json')
		self._md5_fname = os.path.join(self._path, name + '.md5')
		self._f = open(self._fname, 'a')
		self._md5

	def update(self, id_dict, content, **kwargs):
		line = json.dumps(content)
		lines = line.splitlines()
		if (len(lines) > 1):
			line = ' '.join(lines)
		self._f.writelines((line, '\n'))
		md5sum = md5(line).hexdigest()
		self._md5.writelines((md5sum, '\n'))
		#print '>%s: %s' % (self._fname, line)

	def ensure_index(self, name, *args, **kwargs):
		pass

	def close(self):
		pass
		if self._f is not None and not self._f.closed:
			self._f.close()


class _MongoImportFileSetConnectionMock(object):
	def __init__(self, *args):
		self._files = args

	def disconnect(self, *args):
		for open_file in (f for f in self._files if f is not None):
			# close the files
			try:
				open_file.close()
			except ValueError:
				pass


class _MongoImportFileSet(object):
	'''Interface to a set of mongo import files providing almost the same interface as mongoConnection[db_name] does.'''
	def __init__(self, base_path):
		self._path = base_path
		self._files = {}
		self._artists = None
		self._labels = None
		self._releases = None
		self.collection_names = lambda: ('artists', 'labels', 'releases')
		self.connection = _MongoImportFileSetConnectionMock(*(self._artists, self._labels, self._releases))

	def __getitem__(self, key):
		if key in self.collection_names():
			attr = self.__dict__['_' + key]
			if attr is None:
				attr = _MongoImportFile(key, self._path)
			return attr
		raise IndexError('Invalid key: %s' % key)

	def __getattr__(self, name):
		if name in self.collection_names():
			return self[name]
		else:
			raise AttributeError("Unknown attribute '%s'." % name)



class MongoDbExporter(object):
	def __init__(self, mongo_uri):
		'''mongo_uri: mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]'''
		# TODO: if uri is file://path/ - create a json dump for using with mongo import
		self.connect(mongo_uri)

	def connect(self, mongo_uri):
		db_name = None
		u = urlparse.urlparse(mongo_uri)
		if u.scheme == 'file':
			self.db = _MongoImportFileSet(u.netloc + u.path)
		elif u.scheme == 'mongodb':
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
		else:
			raise ValueError("Invalid URI scheme: '%s'. Can only accept 'file' or 'mongodb'" % u.scheme)

	def execute(self, collection, what):
		# have to convert it to json and back because
		# on simple objects couchdb-python throws:
		# TypeError: argument of type 'instance' is not iterable
		# and on dicts:
		# AttributeError: 'dict' object has no attribute 'read'
		doc = json.loads(json.dumps(what, default=jsonizer))
		self.db[collection].update({'id': what.id}, doc, upsert=True)

	def finish(self, completely_done=False):
		collections = self.db.collection_names()
		if 'artists' in collections:
			#self.db.artists.ensure_index('id', background=True)
			self.db.artists.ensure_index('l_name', background=True, unique=True)
		if 'labels' in collections:
			self.db.labels.ensure_index('l_name', background=True, unique=True)
		if 'releases' in collections:
			self.db.releases.ensure_index('id', background=True, unique=True)
			self.db.releases.ensure_index([('l_artist',pymongo.ASCENDING),
				('l_title',pymongo.ASCENDING)],
				background=True)
			self.db.releases.ensure_index('format.name', background=True)
		self.db.connection.disconnect()

	def storeLabel(self, label):
		label.l_name = label.name.lower()
		self.execute('labels', label)

	def storeArtist(self, artist):
		artist.l_name = artist.name.lower()
		self.execute('artists', artist)

	def storeRelease(self, release):
		release.l_artist = release.artist.lower()
		release.l_title = release.title.lower()
		self.execute('releases', release)

