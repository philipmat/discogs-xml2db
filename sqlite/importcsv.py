#!/usr/bin/env python
"""Usage:
    importcsv.py --db=<db> [--batch=<n>] [--fast] PATH ...

Options:
  --db=<db>         path to sqlite database file
  --batch=<n>       number of rows per batch insert [default: 5000]
  --fast            enable faster (less durable) sqlite settings
  PATH              one or more csv files (optionally .bz2)

"""
import bz2
import csv
import os
import pathlib
import sqlite3
import sys

from docopt import docopt

# since we run this as a script, we need to add the parent folder
# so we can import discogsxml2db from it
parent_path = str(pathlib.Path(__file__).absolute().parent.parent)
sys.path.insert(1, parent_path)
from discogsxml2db.exporter import csv_headers  # noqa


def _open_csv(path):
    if path.endswith('.csv'):
        return open(path, newline='', encoding='utf-8')
    if path.endswith('.csv.bz2'):
        return bz2.open(path, mode='rt', newline='', encoding='utf-8')
    return None


def _normalize_header(header):
    if not header:
        return header
    header[0] = header[0].lstrip('\ufeff')
    return header


def _apply_fast_pragmas(db):
    db.execute("PRAGMA synchronous=OFF")
    db.execute("PRAGMA journal_mode=OFF")
    db.execute("PRAGMA temp_store=MEMORY")
    db.execute("PRAGMA foreign_keys=OFF")


def import_csv(path, db, batch_size):
    base, filename = os.path.split(path)
    table, ext = filename.split('.', 1)
    if ext not in ('csv', 'csv.bz2'):
        print("%s can not be imported: not a .csv or .csv.bz2 file" % filename)
        return

    columns = csv_headers.get(table)
    if not columns:
        print("%s can not be imported: unknown table" % filename)
        return

    fp = _open_csv(path)
    if not fp:
        print("%s can not be imported: failed to open" % filename)
        return

    print("importing %s" % filename)
    reader = csv.reader(fp)
    header = _normalize_header(next(reader, None))
    if header is None:
        print("%s can not be imported: empty file" % filename)
        return
    if header != columns:
        print("warning: header mismatch in %s" % filename)

    placeholders = ", ".join(["?"] * len(columns))
    col_list = ", ".join(columns)
    sql = "INSERT INTO {} ({}) VALUES ({})".format(table, col_list, placeholders)

    cursor = db.cursor()
    batch = []
    for row in reader:
        batch.append(row)
        if len(batch) >= batch_size:
            cursor.executemany(sql, batch)
            db.commit()
            batch.clear()

    if batch:
        cursor.executemany(sql, batch)
        db.commit()
    cursor.close()
    fp.close()


arguments = docopt(__doc__, version='0.1')
db_path = arguments['--db']
try:
    batch_size = int(arguments['--batch'])
except ValueError:
    print("error: --batch must be an integer")
    sys.exit(1)

if not db_path:
    print("error: --db is required")
    sys.exit(1)

if batch_size <= 0:
    print("error: --batch must be positive")
    sys.exit(1)

connection = sqlite3.connect(db_path)
if arguments['--fast']:
    _apply_fast_pragmas(connection)

for path in arguments['PATH']:
    if os.path.isfile(path):
        import_csv(os.path.abspath(path), connection, batch_size)
    else:
        print("error: '%s' is not a readable file" % path)

connection.close()
