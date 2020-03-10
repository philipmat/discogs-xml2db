--- artists
ALTER TABLE artist DROP CONSTRAINT IF EXISTS artist_pkey;

--- labels
ALTER TABLE label DROP CONSTRAINT IF EXISTS label_pkey;

--- masters
ALTER TABLE master DROP CONSTRAINT IF EXISTS master_pkey;

--- releases
ALTER TABLE release DROP CONSTRAINT IF EXISTS release_pkey;
ALTER TABLE release_track DROP CONSTRAINT IF EXISTS release_track_pkey;
ALTER TABLE release_track_artist DROP CONSTRAINT IF EXISTS release_track__artist_pkey;
ALTER TABLE release_format DROP CONSTRAINT IF EXISTS release_format_pkey;
ALTER TABLE release_label DROP CONSTRAINT IF EXISTS release_label_pkey;
ALTER TABLE release_artist DROP CONSTRAINT IF EXISTS release_artist_pkey;
