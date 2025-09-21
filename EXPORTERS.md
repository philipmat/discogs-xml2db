# Export Targets

This project ships two exporter stacks: the legacy SAX-based pipeline driven by `discogsparser.py`, and the newer high-throughput CSV pipeline under `speedup/`. Use the flow that matches your downstream tooling and performance needs.

## Legacy `discogsparser.py` exporters

`discogsparser.py` selects exporters through the `-o / --output` flag (see `exporters/__init__.py`). The built-ins are:

- `json`: `exporters/jsonexporter.JsonConsoleExporter` prints one JSON document per entity to stdout. Redirect stdout to persist the stream into a file.
- `pgsql`: `exporters/postgresexporter.PostgresExporter` inserts rows directly into a PostgreSQL database using the supplied connection string.
- `pgdump`: `exporters/postgresexporter.PostgresConsoleDumper` writes the generated `INSERT` statements to stdout so you can capture a ready-to-run SQL dump.
- `couch`: `exporters/couchdbexporter.CouchDbExporter` pushes records into a CouchDB instance over HTTP.
- `mongo`: `exporters/mongodbexporter.MongoDbExporter` either connects to MongoDB (`mongodb://…`) or, when given a `file://` URI, appends newline-delimited JSON dumps plus accompanying MD5 checksum files for later `mongoimport` runs.

All of these honor the optional `-q/--quality` filter so you can restrict exports to specific `data_quality` values.

## Speed-up CSV pipeline

For bulk conversions, prefer `speedup/exporter.py`. It parses Discogs XML dumps with streaming parsers and materialises relational tables as CSV:

- Outputs are plain `.csv` files by default or `.csv.bz2` when `--bz2` is set.
- Each entity (`artist`, `label`, `master`, `release`) writes multiple table files so they can be loaded into PostgreSQL/MySQL helpers found under `speedup/`.
- Typical invocations:
  - Legacy docopt script: `python3 speedup/exporter.py --bz2 --export release dump-dir out/csv`
  - New Click CLI (recommended): `uv run discogs-export export --bz2 --export release dump-dir out/csv`

## Can I export CSV?

Yes. Use the `speedup` exporter for first-class CSV (optionally bz2-compressed) output. The legacy stack focuses on JSON streams plus PostgreSQL, CouchDB, and MongoDB adapters.
