class Artist:
   def __init__(self):
      self.name = ''
      self.realname = ''
      self.images = []
      #self.urls = {'wikipedia':None, 'myspace':None,'other':[]}
      self.urls = []
      self.namevariations = [] 
      self.aliases = []
      self.profile = ''
      self.members = []#MemberNameList, foreign key name, class Artist
      self.groups = []#GroupNameList, foreign key name, class Artist
      #self.artistType = 0 #0 = person, 1 = group 

class Release:
   def __init__(self):
     self.discogs_id = ''
     self.status = ''
     self.title = ''
     self.country = ''
     self.released = ''
     self.notes = ''
     self.genres = []
     self.styles = []
     self.images = []
     self.formats = []
     self.labels = []
     self.anv = '' #used only if artist name is missing
     self.artists = [] #join
     self.artistJoins = [] #release_artist_artist
     self.tracklist = [] #join
     self.extraartists = []

class ArtistJoin:
  def __init__(self):
    self.artist1 = ''
    self.join_relation = ''

class Extraartist:
  def __init__(self):
    self.name = ''
    self.roles = []

class ReleaseLabel:
  def __init__(self):
    self.name = ''
    self.catno = ''

class Label:
  def __init__(self):
    self.name = ''
    self.images = []
    self.contactinfo = ''
    self.profile = ''
    self.parentLabel = ''
    self.urls = []
    self.sublabels = []

class Format:
  def __init__(self):
    self.name = ''
    self.qty = 0
    self.descriptions = []

class Style:
  def __init__(self, name):
    self.name = name
    #self.genres = []

class Genre:
  def __init__(self, name):
    self.name = name
   
class Track:
  def __init__(self):
    self.artists = []
    self.artistJoins = []
    self.extraartists = []
    self.title = ''
    self.duration = ''
    self.position = ''

class ImageInfo:
  def __init__(self):
    self.height = 0      
    self.imageType = None #enum ImageType.PRIMARY or ImageType.SECONDARY
    self.uri = ''    
    self.uri150 = '' 
    self.width = 0   

class ImageType:
  PRIMARY = 0
  SECONDARY = 1

