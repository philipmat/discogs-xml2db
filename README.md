From original source: http://code.google.com/p/discogs-sql-importer/

----

This is a python program for importing the discogs data dumps found at http://www.discogs.com/data/ into a PostgreSQL database.

MySQL or other databases are not supported at the moment, but you are welcome to submit a patch.

Steps to import the datadumps:

1. Download and extract the data dumps
2. Create the empty database: `createdb -U {user-name} discogs`
3. Import the database schema: `psql -U {user-name} -d discogs -f discogs.sql`
4. The XML data dumps often contain control characters and do not have root tags. To fix this run `fix-xml.py _release_`, where release is the release date of the dump, for example `20100201`.
5. Finally import the data with `python discogsparser.py _release_`, where release is the release date of the dump, for example `20100201`


