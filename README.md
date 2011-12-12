
----
# What is it?
This is a python program for importing the discogs data dumps found at http://www.discogs.com/data/ into PostgreSQL, CouchDB, or MongoDB database.

MySQL or other databases are not supported at the moment, but you are welcome to submit a patch.


# Options for `discogsparser.py`

* **Input**: `-d`/`--date` parses all three files (artists, labels, releases) for a given monthly dump:
    * `discogsparser.py -d 20111101` will look for `discogs_20111101_artists.xml`, `discogs_20111101_labels.xml`, and `discogs_20111101_releases.xml` in the current directory;
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
    * `-o mongo -p "file:///path/to/dir/"`: outputs each of the Artists, Labels, Releases into a separate JSON file into the specified directory, `/path/to/dir/` in this case, one line for each. Pass `--ignoreblanks` to `mongoimport` in case extra new-lines are added; you probably also want `--upsert --upseftFields id`.


# Examples:

    discogsparser.py -n 200 -o couch --params http://127.0.0.1:5984/discogs -d 20111101
    discogsparser.py -o mongo -p mongodb://localhost,remote1/discogs discogs_20111101_artists.xml discogs_20111101_releases.xml
    discogsparser.py -o pgsql -p "host=remote1 dbname=discogs user=postgres password=s3cret" discogs_20111101_artists.xml


# How do I use it?
Start by downloading and extracting the data dumps (you can use `get_latest_dump.sh` to get the latest dumps).

Steps to import the data-dumps into PostgreSQL:

1. Create the empty database: `createdb -U {user-name} discogs`
2. Import the database schema: `psql -U {user-name} -d discogs -f discogs.sql`
3. The XML data dumps often contain control characters and do not have root tags. To fix this run `fix-xml.py _release_`, where release is the release date of the dump, for example `20100201`.
4. Finally import the data with `python discogsparser.py -o pgsql -p "dbname=discogs" pgsql _release_`, where release is the release date of the dump, for example `20100201`

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
    $ mongoimport -d discogs -c releases --ignoreBlanks releases.json

    # use the mongo command to connect to the database and create the indexes you need, the ids at a minimum
    # but you'll probably want l_name as well
    $ mongo discogs
    > db.artists.ensureIndex({id:1}, {background:true,unique:true})
    > db.artists.ensureIndex({l_name:1}, {background:true})
    > db.releases.ensureIndex({id:1}, {background:true,unique:true})
    > db.releases.ensureIndex({l_title:1, l_artist:1}, {background:true, unique:true})

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

* v0.60 - support for CouchDB and MongoDB
* v0.50 - command line parameters controlling various import options
* v0.15 - Original import of discogs-sql-importer
