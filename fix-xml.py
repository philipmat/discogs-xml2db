from __future__ import with_statement
import re
import sys
import os
from contextlib import nested

def clean(filename):
  #delete any byte that's between 0x00 and 0x1F except 0x09 (tab), 0x0A (LF), and 0x0D (CR). 
  ctrlregex = re.compile(r'[\x01-\x08|\x0B|\x0C|\x0E-\x1F]')
  
  try:
    os.rename(filename, "%s.old" %filename)
  except:
    print "Did not rename"

  with nested(open(filename, "wb" ), open(filename+".old", "rb" )) as (destination, source):
	
    counter = 0
    for line in source:
      rObj = re.search(ctrlregex, line)
      counter += 1
      if rObj is not None:
        print counter
        newLine = re.sub(ctrlregex, '', line)
        destination.write(newLine)
      else:
        destination.write(line)	
  
  os.remove("%s.old" %filename)

def usage():
  print "Usage: python fix-xml.py release, where release is for example 20091101"
  sys.exit()

def main(argv):
  if len(argv) == 0 or len(argv[0]) != 8:
    usage()
  try:
    int(argv[0])
  except ValueError:
    usage() 
    sys.exit()

  release = argv[0]

  filename = 'discogs_%s_labels.xml' % release
  clean(filename)

  filename = 'discogs_%s_releases.xml' % release
  clean(filename)

  filename = 'discogs_%s_artists.xml' % release
  clean(filename)

if __name__ == '__main__':
	main(sys.argv[1:])

