# Index Issues

## release_track_idx_title (PostgreSQL)

- When we run `speedup/postgresql/sql/CreateIndexes.sql` against the 202401 Discogs dump, `CREATE INDEX release_track_idx_title ON release_track (title);` fails with `ERROR:  index row size ... exceeds btree version 4 maximum 2704`.
- After the failure I inspected the table with `SELECT id, release_id, sequence, length(title) AS title_len FROM release_track ORDER BY title_len DESC LIMIT 10;` and found several rows where `title_len` was well over the 2.7KB btree limit (the top offenders were in the 5–6KB range, mostly spoken-word liner notes copied into the title field).
- Because Postgres aborts the entire script as soon as that statement errors, leaving the clause in place prevents every downstream index in the speedup pipeline from being created. Commenting it out lets the rest of the index build complete so imports can proceed.
- We rarely filter on the raw `title` column, and the workloads that do tend to rely on pattern searches that will need an alternative such as a trigram GIN or an expression index. Until we design a version that works with oversized values, keeping the btree definition disabled avoids the fatal migration.
