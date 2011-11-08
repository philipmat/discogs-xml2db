import json

def jsonizer(obj):
	'''Assists in serializing models to JSON.

	>>> json.dumps(an_artist, default=jsonizer)
	'{"_type" : "Artist", "name" : ...
	'''
	j_dict = { '_type' : obj.__class__.__name__ }
	j_dict.update(obj.__dict__)
	return j_dict 


class JsonConsoleExporter:
	def __init__(self, params):
		pass
	
	def finish(self, completely_done = False):
		pass
	
	def _store(self, what):
		return json.dumps(what, default=jsonizer)

	def storeArtist(self, artist):
		j = self._store(artist)
		print j
	
	def storeLabel(self, label):
		j = self._store(label)
		print j

	def storeRelease(self, release):
		j = self._store(release)
		print j

