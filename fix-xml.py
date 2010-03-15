from __future__ import with_statement
import re
import sys
import fileinput

def append_end_tag(end_tag, filename):
  with open(filename,'a') as f:
    f.write(end_tag)

def clean_and_append_start_tag(start_tag, filename):
  #delete any byte that's between 0x00 and 0x1F except 0x09 (tab), 0x0A (LF), and 0x0D (CR). 
  ctrlregex = re.compile(r'[\x01-\x08|\x0B|\x0C|\x0E-\x1F]')

  f = fileinput.input(filename,inplace=1)

  #append start tag to the first line
  first_line = f.readline()
  sys.stdout.write("%s\n%s" % (start_tag, first_line))

  counter = 0
  for line in f:
    rObj = re.search(ctrlregex, line)
    counter += 1
    sys.stderr.write(str(counter)+'\n')
    if rObj is not None:
      newLine = re.sub(ctrlregex, '', line)
      sys.stdout.write(newLine)
    else:
      sys.stdout.write(line)

def usage():
  print "Usage: python fix-xml.py relase, where release is for example 20091101"
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
  append_end_tag('</labels>', filename)
  clean_and_append_start_tag('<labels>', filename)

  filename = 'discogs_%s_releases.xml' % release
  append_end_tag('</releases>', filename)
  clean_and_append_start_tag('<releases>', filename)

  filename = 'discogs_%s_artists.xml' % release
  append_end_tag('</artists>', filename)
  clean_and_append_start_tag('<artists>', filename)

if __name__ == '__main__':
	main(sys.argv[1:])

