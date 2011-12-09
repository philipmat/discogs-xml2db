import json
import sys

f = open(sys.argv[1], 'rb')
while 1:
	lines = f.readlines(10000)
	if not lines:
		break
	for line in lines:
		try:
			d = json.loads(line)
		except ValueError as e:
			print "ValueError : %s on line:\n%s" % (e, line)
		if d:
			n = u'%s |%s' % (d['l_name'], d['artist_id'])
			print n.encode('utf-8') 

f.close()
