From original source: http://code.google.com/p/discogs-sql-importer/

----

This is a python program for importing the discogs data dumps found at http://www.discogs.com/data/ into a PostgreSQL database.

MySQL or other databases are not supported at the moment, but you are welcome to submit a patch.

Steps to import the datadumps (into PostgreSQL):

1. Download and extract the data dumps
2. Create the empty database: `createdb -U {user-name} discogs`
3. Import the database schema: `psql -U {user-name} -d discogs -f discogs.sql`
4. The XML data dumps often contain control characters and do not have root tags. To fix this run `fix-xml.py _release_`, where release is the release date of the dump, for example `20100201`.
5. Finally import the data with `python discogsparser.py -o pgsql -p "dbname=discogs" pgsql _release_`, where release is the release date of the dump, for example `20100201`

Options for `discogsparser.py`:

* **Input**: `-d`/`--date` parses all three files (artists, labels, releases) for a given monthly dump:
    `discogsparser.py -d 20111101` will look for `discogs_20111101_artists.xml`, `discogs_20111101_labels.xml`, and `discogs_20111101_releases.xml` in the current directory;
* **Input**: parse only specific file(s):
    `discogsparser.py /tmp/discogs_20111101_artists.xml /tmp/discogs_20111101_releases.xml` - will only parse the artist and release dumps from the `/tmp` directory;
* **Output**: `-o json` dumps records as JSON to the console:
    `discogsparser.py -o json discogs_20111101_artists.xml`;
* **Output**: `-n 20` only process a given number of records 
    `discogsparser.py -n 20 -o json -d 20111101` will dump 20 records from each of the three monthly dumps as JSON to the console;
* **Output**: `-o`/`--output` for output format (aka exporter) and `-p`/`--params` for parameters to the specific exporter
    * `-o json`, no `-p`: dumps JSON to the console;
    * `-o pgsql -p "connection string"`: exports into a PostgreSQL database. See [The psycopg2 module content](http://initd.org/psycopg/docs/module.html) for connection string documentation.
    * `-o couch -p "couch URI"`: exports to a CouchDB server running on localhost on port 5984 into a database named `discogs`;
    * `-o mongo -p "mongodb connection URI"`: connects, with `user` and `pass`, to a MongoDB server running on localhost, and into a database named `discogs`. See [Standard Connection String Format](http://www.mongodb.org/display/DOCS/Connections) in the MongoDB docs.


Examples:

    discogsparser.py -n 200 -o couch --params http://127.0.0.1:5984/discogs -d 20111101
    discogsparser.py -o mongo -p mongodb://localhost,remote1/discogs discogs_20111101_artists.xml discogs_20111101_releases.xml
    discogsparser.py -o pgsql -p "server=remote1 dbname=discogs user=postgres password=s3cret" discogs_20111101_artists.xml


