mkdir csv
python3 exporter.py --export release --export master --export label --export artist .. csv
python3 discogs-psql.py < sql/DropTables.sql
python3 discogs-psql.py < sql/CreateTables.sql
python3 discogs-import.py csv/*
python3 discogs-psql.py < sql/CreatePrimaryKeys.sql
python3 discogs-psql.py < sql/CreateFKConstraints.sql
python3 discogs-psql.py < sql/CreateIndexes.sql
