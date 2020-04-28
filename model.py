class Artist:
	def __init__(self):
		self.id = 0
		self.name = ''
		self.realname = ''
		self.images = []
		# self.urls = {'wikipedia':None, 'myspace':None,'other':[]}
		self.urls = []
		self.namevariations = []
		self.aliases = []
		self.profile = ''
		self.members = []  # MemberNameList, foreign key name, class Artist
		self.groups = []  # GroupNameList, foreign key name, class Artist
		# self.artistType = 0 #0 = person, 1 = group
		# self.artist_id = ''


class Release:
	def __init__(self):
		self.id = 0
		self.master_id = 0
		self.status = ''
		self.title = ''
		self.country = ''
		self.released = ''
		self.notes = ''
		self.barcode = None
		self.genres = []
		self.styles = []
		self.images = []
		self.formats = []
		self.labels = []
		self.companies = []
		self.artistJoins = []
		self.tracklist = []
		self.extraartists = []
		# self.indentifiers = [] #


class Master:
	def __init__(self):
		self.id = 0
		# self.status = ''
		self.title = ''
		self.main_release = 0
		self.year = 0
		self.notes = ''
		self.genres = []
		self.styles = []
		self.images = []
		self.anv = ''  # used only if artist name is missing
		self.artist = ''
		self.artists = []  # join
		self.artistJoins = []  # release_artist_artist
		self.extraartists = []


class ArtistJoin:
	def __init__(self):
		self.artist_id = 0
		self.artist_name = ''
		self.anv = None
		self.join_relation = None


class Extraartist:
	def __init__(self):
		self.artist_id = 0
		self.artist_name = ''
		self.anv = None
		self.roles = []


class ReleaseLabel:
	def __init__(self):
		self.name = ''
		self.catno = ''


class Label:
	def __init__(self):
		self.id = 0
		self.name = ''
		self.images = []
		self.contactinfo = ''
		self.profile = ''
		self.parentLabel = ''
		self.parentId = 0
		self.urls = []
		self.sublabels = []


class Company:
	def __init__(self):
		self.id = 0
		self.name = ''
		self.catno = ''
		self.type = 0
		self.type_name = ''


class Format:
	def __init__(self):
		self.name = ''
		self.qty = 0
		self.text = ''
		self.descriptions = []


class Style:
	def __init__(self, name):
		self.name = name
		# self.genres = []


class Genre:
	def __init__(self, name):
		self.name = name


class Track:
	def __init__(self):
		self.artistJoins = []
		self.extraartists = []
		self.title = ''
		self.duration = ''
		self.position = ''


class ImageInfo:
	def __init__(self):
		self.height = 0
		self.imageType = None  # enum ImageType.PRIMARY or ImageType.SECONDARY
		self.uri = ''
		self.uri150 = ''
		self.width = 0


class ImageType:
	PRIMARY = 0
	SECONDARY = 1


class ParserStopError(Exception):
	"""Raised by a parser to signal that it wants to stop parsing."""
	def __init__(self, count):
		self.records_parsed = count
