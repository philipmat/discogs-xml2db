#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Usage:
  run.py [--bz2] [--dry-run] [--limit=<lines>] [--debug] [--apicounts] INPUT [OUTPUT] [--export=<entity>]...

Options:
  --bz2                 Compress output files using bz2 compression library.
  --limit=<lines>       Limit export to some number of entities
  --export=<entity>     Limit export to some entities (repeatable)
  --debug               Turn on debugging prints
  --apicounts           Check entities counts with Discogs API
  --dry-run             Do not write

"""
import sys

from docopt import docopt
from discogsxml2db.exporter import main


if __name__ == '__main__':
    arguments = docopt(__doc__, version='Discogs-to-SQL exporter')
    sys.exit(main(arguments))
