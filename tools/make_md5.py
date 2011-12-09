from hashlib import md5
import sys


def main(argv):
	if not argv:
		argv = ['-']

	for arg in argv:
		file = None
		if arg == '-':
			file = sys.stdin
		else:
			file = fopen(arg, 'rb')
		if file:
			while 1:
				lines = file.readlines(10000)
				if not lines:
					break
				for line in lines:
					line = line.rstrip('\r\n')
					try:
						id,json = line.split('|',1)
						print "%s:%s" % (id,md5(json).hexdigest())
					except ValueError as ve:
						print "ValueError on line %s." % line
			file.close()


if __name__ == '__main__':
	main(sys.argv[1:])
