from datetime import date
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
		self._previous_md5_fname = os.path.join(self._path, name + '_previous.md5')
		self._f = open(self._fname, 'a')

	def update(self, id_dict, content, **kwargs):
		line = json.dumps(content)
		lines = line.splitlines()
		if (len(lines) > 1):
			line = ' '.join(lines)
		self._f.writelines((line, '\n'))
		#print '>%s: %s' % (self._fname, line)

	def ensure_index(self, name, *args, **kwargs):
		pass

	def close(self):
		pass
		if self._f is not None and not self._f.closed:
			self._f.close()
		if self._md5 is not None and not self._md5.closed:
			self._md5.close()


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
		self._masters = None
		self.collection_names = lambda: ('artists', 'labels', 'releases', 'masters')
		self.connection = _MongoImportFileSetConnectionMock(*(self._artists, self._labels, self._releases, self._masters))

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


class _IdHashPairs(object):
	'''Cache of the current records in the database indexed by id'''
	def __init__(self, path):
		self._path = os.path.abspath(os.path.expanduser(path))
		self._hashes = {'artists': None, 'labels': None, 'releases': None}
		self._name = lambda x: os.path.join(self._path, '%s.md5' % x)

	def _load(self, name):
		if self._hashes[name] is None:
			try:
				fname = self._name(name)
				with open(fname, 'rb') as f:
					name_hash = {}
					while 1:
						lines = f.readlines(10000)
						if not lines:
							break
						for line in lines:
							line = line.rstrip('\r\n')
							old_id, md5 = line.split(':')
							name_hash[int(old_id)] = md5
					self._hashes[name] = name_hash
			except IOError as e:
				self._hashes[name] = {}

	def _one_line(self, line):
		lines = line.splitlines()
		return ' '.join(lines) if (len(lines) > 1) else line

	def is_uniq(self, collection, id, json_string):
		self._load(collection)
		new_md5 = md5(self._one_line(json_string)).hexdigest()
		i_id = int(id)
		if i_id in self._hashes[collection]:
			old_md5 = self._hashes[collection][i_id]
			if new_md5 == old_md5:
				return (False, new_md5)
			else:
				return (True, new_md5)
		else:
			return (True, new_md5)

	def process(self, collection, id, md5_digest):
		self._load(collection)
		i_id = int(id)
		self._hashes[collection][i_id] = md5_digest

	def finish(self):
		# write hashes back to disk
		for name in self._hashes:
			try:
				fname = self._name(name)
				if self._hashes[name]:
					with open(fname, 'w') as f:
						for id, hash in self._hashes[name].iteritems():
							f.writelines(('%s:%s' % (id, hash), '\n'))
			except IOError as e:
				print 'IOError writing out %s: %s' % (name, e)


class MongoDbExporter(object):
	def __init__(self, mongo_uri, data_quality=[]):
		'''mongo_uri: mongodb://[username:password@]host1[:port1],...[,hostN[:portN]][/[database][?options]]'''
		# TODO: if uri is file://path/ - create a json dump for using with mongo import
		self.min_data_quality = data_quality
		self._options = {}
		self._quick_uniq = None
		self.connect(mongo_uri)
		# don't submit to mongo values that already exist - faster to compute it here then in mongo

	def connect(self, mongo_uri):
		db_name, options = None, {}
		u = urlparse.urlparse(mongo_uri)
		if u.scheme == 'file':
			path = u.path
			if '?' in u.path:
				path, self._options = u.path.split('?', 1)
				self._options = urlparse.parse_qs(self._options) if self._options else {}
			path = u.netloc + path
			self.db = _MongoImportFileSet(path)
			if 'uniq' in self._options and 'md5' in self._options['uniq']:
				self._quick_uniq = _IdHashPairs(path)
		elif u.scheme == 'mongodb':
			if '?' in u.path and u.query == '':
				#url didn't parse it properly u.path is '/dbname?options
				db_name, self._options = u.path.split('?', 1)
				self._options = urlparse.parse_qs(self._options) if self._options else {}
			else:
				db_name = u.path
			if db_name.startswith('/'):
				db_name = db_name[1:]
			#print 'Connecting to db %s on %s with options.' % (db_name, mongo_uri, options)
			mongo = pymongo.Connection(mongo_uri)
			self.db = mongo[db_name]
			if 'uniq' in self._options and 'md5' in self._options['uniq']:
				self._quick_uniq = False
		else:
			raise ValueError("Invalid URI scheme: '%s'. Can only accept 'file' or 'mongodb'" % u.scheme)

	def _is_uniq(self, collection, id, json_string):
		if self._quick_uniq is not None:
			return self._quick_uniq.is_uniq(collection, id, json_string)
		return (True, None)

	def _store_processed(self, collection, id, md5_digest):
		if self._quick_uniq is not None:
			self._quick_uniq.process(collection, id, md5_digest)

	def good_quality(self, what):
		if len(self.min_data_quality):
			return what.data_quality.lower() in self.min_data_quality
		return True

	def execute(self, collection, what):
		if not self.good_quality(what):
			# print "Bad quality: %s for %s" % (what.data_quality, what.id)
			return
		# have to convert it to json and back because
		# on simple objects couchdb-python throws:
		# TypeError: argument of type 'instance' is not iterable
		# and on dicts:
		# AttributeError: 'dict' object has no attribute 'read'
		json_string = json.dumps(what, default=jsonizer)
		uniq, md5 = self._is_uniq(collection, what.id, json_string)
		if uniq:
			doc = json.loads(json_string)
			doc['updated_on'] = "%s" % date.today()
			self.db[collection].update({'id': what.id}, doc, upsert=True)
			self._store_processed(collection, what.id, md5)

	def finish(self, completely_done=False):
		collections = self.db.collection_names()
		if 'artists' in collections:
			#self.db.artists.('id', background=True)
			self.db.artists.ensure_index('id', background=True, unique=True)
			# should be unique=True, but can't seem to offer a true guarantee
			self.db.artists.ensure_index('l_name', background=True)
		if 'labels' in collections:
			self.db.labels.ensure_index('id', background=True, unique=True)
			self.db.labels.ensure_index('l_name', background=True)
		if 'releases' in collections:
			self.db.releases.ensure_index('id', background=True, unique=True)
			self.db.releases.ensure_index([('l_artist', pymongo.ASCENDING),
				('l_title', pymongo.ASCENDING)],
				background=True)
			self.db.releases.ensure_index('format.name', background=True)
		if 'masters' in collections:
			self.db.masters.ensure_index('id', background=True, unique=True)
			self.db.masters.ensure_index('l_title', background=True)
			self.db.masters.ensure_index('main_release', background=True, unique=True)
		self.db.connection.disconnect()
		if self._quick_uniq is not None:
			self._quick_uniq.finish()

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

	def storeMaster(self, master):
		master.l_artist = master.artist.lower()
		master.l_title = master.title.lower()
		self.execute('masters', master)
