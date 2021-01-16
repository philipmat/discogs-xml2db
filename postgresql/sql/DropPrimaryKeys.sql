--- artists
ALTER TABLE artist DROP CONSTRAINT IF EXISTS artist_pkey;
ALTER TABLE artist_url DROP CONSTRAINT IF EXISTS artist_url_pkey;
ALTER TABLE artist_namevariation DROP CONSTRAINT IF EXISTS artist_namevariation_pkey;

--- labels
ALTER TABLE label DROP CONSTRAINT IF EXISTS label_pkey;
ALTER TABLE label_url DROP CONSTRAINT IF EXISTS label_url_pkey;

--- masters
ALTER TABLE master DROP CONSTRAINT IF EXISTS master_pkey;
ALTER TABLE master_artist DROP CONSTRAINT IF EXISTS master_artist_pkey;
ALTER TABLE master_video DROP CONSTRAINT IF EXISTS master_video_pkey;
ALTER TABLE master_genre DROP CONSTRAINT IF EXISTS master_genre_pkey;
ALTER TABLE master_style DROP CONSTRAINT IF EXISTS master_style_pkey;

--- releases
ALTER TABLE release DROP CONSTRAINT IF EXISTS release_pkey;
ALTER TABLE release_artist DROP CONSTRAINT IF EXISTS release_artist_pkey;
ALTER TABLE release_label DROP CONSTRAINT IF EXISTS release_label_pkey;
ALTER TABLE release_genre DROP CONSTRAINT IF EXISTS release_genre_pkey;
ALTER TABLE release_format DROP CONSTRAINT IF EXISTS release_format_pkey;
ALTER TABLE release_track DROP CONSTRAINT IF EXISTS release_track_pkey;
ALTER TABLE release_track_artist DROP CONSTRAINT IF EXISTS release_track__artist_pkey;
ALTER TABLE release_identifier DROP CONSTRAINT IF EXISTS release_identifier_pkey;
ALTER TABLE release_video DROP CONSTRAINT IF EXISTS release_video_pkey;
ALTER TABLE release_company DROP CONSTRAINT IF EXISTS release_company_pkey;
