#!/usr/bin/env python
"""Usage:
    mysql_loadcsv.py [--config=<config>] PATH ...

Options:
  --config=<config>      path to database config file
  PATH                   one or more csv files

"""

# This script imports csv files into a mysql database. It is meant as a pure
# python implementation of importcsv.sh for use on systems where the bash shell
# is not available.
#
# It lacks two major features that the bash version has:
# - it can not import compressed csv files
# - it does not display the importing progress
#
# Both features can not be implemented because we need to pass a path to mysql:
#   sql = 'load data from PATH into table TABLE'
#   cursor.execute(sql)
# These features could be implemented for postgresql because psycopg2 allows
# for a file object to be passed in addition to a SQL string:
#    sql = "copy TABLE from stdin"
#    file = uncompress(monitor_progress(PATH))
#    cursor.copy_expert(sql, file)


import os
import sys
import configparser
from docopt import docopt
try:
    import mysql.connector
except ImportError:
    print("error: no mysql connector. Please install mysql-connector-python using pip.")
    sys.exit()


def read_config(path):
    with open(path) as f:
        file_content = '[general]\n' + f.read()
    config_parser = configparser.RawConfigParser()
    config_parser.read_string(file_content)
    config = config_parser["general"]
    return config


def import_csv(path, mysql_config):
    base, filename = os.path.split(path)
    table, ext = filename.split('.', 1)
    if ext != 'csv':
        print('%s can not be imported: not a .csv file' % filename)
        return
    fp = open(path)
    print("importing %s" % filename)
    cols = fp.readline()[:-1]
    sql = """load data local infile '%s'
           into table %s
           fields terminated by ',' ESCAPED BY '' OPTIONALLY ENCLOSED BY '\\"'
           lines terminated by '\\n'
           IGNORE 1 LINES
           (%s);""" % (path, table, cols)
    connection = mysql.connector.connect(
        host=mysql_config['host'],
        database=mysql_config['database'],
        user=mysql_config['user'],
        password=mysql_config['password'],
        allow_local_infile=True)
    cursor = connection.cursor()
    cursor.execute(sql)
    connection.commit()
    cursor.close()
    connection.close()


arguments = docopt(__doc__, version='0.1')
paths = arguments['PATH']

if arguments['--config']:
    mysql_config = read_config(arguments['--config'])
else:
    root = os.path.realpath(os.path.dirname(__file__))
    mysql_config = read_config(os.path.join(root, 'mysql.conf'))

for path in paths:
    if os.path.isfile(path):
        import_csv(path, mysql_config)
    else:
        print("error: '%s' is not a readable file" % path)
