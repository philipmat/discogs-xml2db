#!/usr/bin/env python
import bz2
import sys
import os

from psycopg2 import sql

from dbconfig import connect_db, Config, columns


def load_csv(filename, db):
    print("Importing data from {}".format(filename))
    base, fname = os.path.split(filename)
    table, ext  = fname.split('.', 1)
    if ext.startswith('csv'):
        q = sql.SQL("COPY {} ({}) FROM STDIN WITH CSV HEADER").format(
                sql.Identifier(table),
                sql.SQL(', ').join(map(sql.Identifier, columns[table])))

    if ext == 'csv':
        fp = open(filename)
    elif ext == 'csv.bz2':
        fp = bz2.BZ2File(filename)

    cursor = db.cursor()
    cursor.copy_expert(q, fp)
    db.commit()

root = os.path.realpath(os.path.dirname(__file__))
config = Config(os.path.join(root, 'discogs.conf'))
db = connect_db(config)

for filename in sys.argv[1:]:
    load_csv(os.path.abspath(filename), db)
