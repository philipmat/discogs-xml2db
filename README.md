----
# What is it?
This is a python program for importing the discogs data dumps found at http://www.discogs.com/data/ into PostgreSQL, CouchDB, or MongoDB database.

MySQL or other databases are not supported at the moment, but you are welcome to submit a patch.

**discogs-xml2db works with Python 2.7**.  
It may work with 2.6 or 3.0, but probably not (I don't know, I didn't test). It definitely doesn't work with 2.5.  

discogs-xml2db makes use of the following modules (some are standard in 2.7, some you'll need to `pip install`):
* xml.sax - for handling the source XML files
* argparse - for parsing the command line arguments
* json - to save files in JSON format or talk to some back-ends
* couchdb - if you use the CouchDB back-end (probably not)
* pymongo - if you use the MongoDB back-end (best option)
* psycopg2 - for the PostgreSQL back-end (not your best option)


# Options for `discogsparser.py`

* **Input**: `-d`/`--date` parses all three files (artists, labels, masters, releases) for a given monthly dump:
    * `discogsparser.py -d 20111101` will look for `discogs_20111101_artists.xml`, `discogs_20111101_labels.xml`, `discogs_20111101_masters.xml`, and `discogs_20111101_releases.xml` in the current directory;
* **Input**: parse only specific file(s):
    * `discogsparser.py /tmp/discogs_20111101_artists.xml /tmp/discogs_20111101_releases.xml` - will only parse the artist and release dumps from the `/tmp` directory;
* **Input**: `-i`/`--ignore-unknown-tags`: ignores new fields that may appear in the XML as the dump format evolves  
    * `discogsparser.py -i` - will display the unknown tags at the end of parsing each file, e.g.: `Encountered some unknown Release tags: [u'data_quality', u'videos', u'video', u'identifiers', u'identifier']`
* **Output**: `-o json` dumps records as JSON to the console:
    * `discogsparser.py -o json discogs_20111101_artists.xml`;
* **Output**: `-n 20` only process a given number of records
    * `discogsparser.py -n 20 -o json -d 20111101` will dump 20 records from each of the three monthly dumps as JSON to the console;  
* **Output**: `-o`/`--output` for output format (aka exporter) and `-p`/`--params` for parameters to the specific exporter
    * `-o json`, no `-p`: dumps JSON to the console;
    * `-o pgsql -p "connection string"`: exports into a PostgreSQL database. See [The psycopg2 module content](http://initd.org/psycopg/docs/module.html) for connection string documentation.
    * `-o couch -p "couch URI"`: exports to a CouchDB server running on localhost on port 5984 into a database named `discogs`;
    * `-o mongo -p "mongodb://localhost/discogs"`: connects, with `user` and `pass`, to a MongoDB server running on localhost, and into a database named `discogs`. See [Standard Connection String Format](http://www.mongodb.org/display/DOCS/Connections) in the MongoDB docs.
    * `-o mongo -p "file:///path/to/dir/"`: outputs each of the Artists, Labels, Masters, Releases into a separate JSON file into the specified directory, `/path/to/dir/` in this case, one line for each. Pass `--ignoreblanks` to `mongoimport` in case extra new-lines are added; you probably also want `--upsert --upseftFields id`.
* **Output**: `-q`/`--quality` - imports only items with the specified data_quality. Takes in a comma-separated list of values for multiple entries. Valid values: 'Needs Vote', 'Complete And Correct', 'Correct', 'Needs Minor Changes', 'Needs Major Changes', 'Entirely Incorrect', 'Entirely Incorrect Edit'.
    * `discogsparser.py -q 'Complete And Correct,Correct,Needs Minor Changes'`


# Examples:

    python discogsparser.py -n 200 -o couch --params http://127.0.0.1:5984/discogs -d 20111101
    python discogsparser.py -o mongo -p mongodb://localhost,remote1/discogs discogs_20111101_artists.xml discogs_20111101_releases.xml
    python discogsparser.py -o pgsql -p "host=remote1 dbname=discogs user=postgres password=s3cret" discogs_20111101_artists.xml
    python discogsparser.py -o pgsql -p "host=remote1 dbname=discogs user=postgres password=s3cret" -d 20140501


# How do I use it?
Start by downloading the data dumps (you can use `get_latest_dump.sh` to get the latest dumps).

Steps to import the data-dumps into PostgreSQL:

1. Unzip the dumps to the source directory: `gunzip discogs_20140501_*.xml.gz`
2. Login as database adminstrator user if not already, i.e: `sudo su - postgres`
3. Create discogs user and empty discogs database `createuser discogs; createdb -U discogs discogs`
4. Exit from adminstrator account
5. Import the database schema: `psql -U discogs -d discogs -f create_tables.sql`
6. The XML data dumps often contain control characters and do not have root tags. To fix this run `python fix-xml.py release`, where release is the release date of the dump, for example `20100201`.
7. Import the data with `python discogsparser.py -o pgsql -p "dbname=discogs user=discogs" -d release`, where release is the release date of the dump, for example `20100201`, this will take some time, for example takes 15 hours on my linux server with SSD
8. Run additional Sql fixes (such as removing duplicate rows): `psql -U discogs discogs -f fix_db.sql`
9. Create Database indexes: `psql -U discogs discogs -f create_indexes.sql`

To import data into MongoDB you have two choices: direct import or dumping the records to JSON and then using `mongoimport`. The latter is considerably faster, particularly for the initial import.

To import directly into MongoDB, specify a `mongodb://` scheme, but be aware that the process is not overly quick.  You might find yourself running the initial import for days. 

The JSON dump method is considerably faster, yet in either case you could take advantage of an option to import only the records that have changed from the previous import. 
The mongo parser will store MD5 hashes of all records it parsed and it can re-use these hashes on subsequent imports, provided you keep the `.md5` files.

To perform a direct import:

    discogsparser.py -i -o mongo -p "mongodb://localhost/discogs?uniq=md5" -d 20111101 


The JSON dump route requires that you specify a `file://` scheme and a location where the intermediate files are to be stored 
(you'll need space - these files are about the same size as the original XMLs):

    $ discogsparser.py -i -o mongo -p "file:///tmp/discogs/?uniq=md5" -d 20111101 
    # this results in 2 files creates for each class, e.g. an artists.json file and an artists.md5 file

    $ mongoimport -d discogs -c artists --ignoreBlanks artists.json
    $ mongoimport -d discogs -c labels --ignoreBlanks labels.json
    $ mongoimport -d discogs -c masters --ignoreBlanks masters.json
    $ mongoimport -d discogs -c releases --ignoreBlanks releases.json

    # use the mongo command to connect to the database and create the indexes you need, the ids at a minimum
    # but you'll probably want l_name as well
    $ mongo discogs
    > db.artists.ensureIndex({id:1}, {background:true,unique:true})
    > db.artists.ensureIndex({l_name:1}, {background:true})
    > db.releases.ensureIndex({id:1}, {background:true,unique:true})
    > db.releases.ensureIndex({l_title:1, l_artist:1}, {background:true, unique:true})
    # etc

    # now import the next month using --upsert:
    $ mongoimport -d discogs -c artists --ignoreBlanks --upsert --upsertFields 'id' artists.json
    

To give you an idea of sizes, the November 10th, 2011 file sizes are: 
artists (2,149,473 records) - XML: 417MB, JSON: 554M; 
labels (275,065) - XML: 56MB, JSON: 70M; 
releases (2,779,084) - XML: 7.5GB, JSON: 6.1GB. 

The December 1st XMLs are a bit bigger, e.g. 422MB for artists. 
However, if you imported the November XMLs with `?uniq=md5`, the December JSON files are: 
artists - 18MB (48,852 records changed from November), 
labels - 3.2MB (11,997 records),
releases - 256MB (98,287).



# Credits

Original project: [discogs-sql-importer](http://code.google.com/p/discogs-sql-importer/)

# Some sort of changelog

* v0.80 - now importing masters
* v0.70 - Ids as primary identifiers for artists, labels, and releases (not FKs though).
  MongoDB can now do diff imports when using the `?uniq=md5` option.
* v0.60 - support for CouchDB and MongoDB
* v0.50 - command line parameters controlling various import options
* v0.15 - Original import of discogs-sql-importer
