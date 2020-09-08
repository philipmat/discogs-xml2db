#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Usage:
  run.py [--bz2] [--dry-run] [--limit=<lines>] [--debug] [--apicounts] [--output=<dir>] <INPUT_FILE> <INPUT_FILE>...
  run.py [--bz2] [--dry-run] [--limit=<lines>] [--debug] [--apicounts] [--output=<dir>] INPUT_DIR [--export=<entity>]...

Options:
  --bz2                 Compress output files using bz2 compression library.
  --limit=<lines>       Limit export to some number of entities (all otherwise)
  --export=<entity>     Limit export to some entities (repeatable).
                        Entity is one of: artist, label, master, release.
  --debug               Turn on debugging prints
  --apicounts           Check entities counts with Discogs API
  --dry-run             Do not write csv files.
  --output=<dir> Where to write the csv files. Defaults to current dir.

"""
import sys

from docopt import docopt
from discogsxml2db.exporter import main


if __name__ == '__main__':
    arguments = docopt(__doc__, version='Discogs-to-SQL exporter')
    if arguments["--debug"]:
        print(arguments)
    sys.exit(main(arguments))
