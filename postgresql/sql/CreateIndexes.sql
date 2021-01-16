--- artists
CREATE INDEX IF NOT EXISTS artist_url_idx_artist ON artist_url (artist_id);
CREATE INDEX IF NOT EXISTS artist_namevariation_idx_artist ON artist_namevariation (artist_id);
CREATE INDEX IF NOT EXISTS artist_alias_idx_artist ON artist_alias (artist_id);
CREATE INDEX IF NOT EXISTS group_member_idx_group ON group_member (group_artist_id);
CREATE INDEX IF NOT EXISTS group_member_idx_member ON group_member (member_artist_id);

--- labels
CREATE INDEX IF NOT EXISTS label_idx_parent_label ON label (parent_id);
CREATE INDEX IF NOT EXISTS label_url_idx_url ON label_url (label_id);

--- masters
CREATE INDEX IF NOT EXISTS master_artist_idx_master ON master_artist (master_id);
CREATE INDEX IF NOT EXISTS master_artist_idx_artist ON master_artist (artist_id);
CREATE INDEX IF NOT EXISTS master_video_idx_master ON master_video (master_id);
CREATE INDEX IF NOT EXISTS master_genre_idx_master ON master_genre (master_id);
CREATE INDEX IF NOT EXISTS master_style_idx_master ON master_style (master_id);

--- releases
CREATE INDEX IF NOT EXISTS release_idx_master ON release (master_id);
CREATE INDEX IF NOT EXISTS release_artist_idx_release ON release_artist (release_id);
CREATE INDEX IF NOT EXISTS release_artist_idx_artist ON release_artist (artist_id);
CREATE INDEX IF NOT EXISTS release_label_idx_release ON release_label (release_id);
CREATE INDEX IF NOT EXISTS release_label_idx_label ON release_label (label_id);
CREATE INDEX IF NOT EXISTS release_genre_idx_release ON release_genre (release_id);
CREATE INDEX IF NOT EXISTS release_style_idx_release ON release_style (release_id);
CREATE INDEX IF NOT EXISTS release_format_idx_release ON release_format (release_id);
CREATE UNIQUE INDEX IF NOT EXISTS release_track_uidx_track on release_track(track_id);
CREATE INDEX IF NOT EXISTS release_track_idx_release ON release_track (release_id);
CREATE INDEX IF NOT EXISTS release_track_idx_sequence ON release_track (sequence);
CREATE INDEX IF NOT EXISTS release_track_idx_parent ON release_track (parent);
CREATE INDEX IF NOT EXISTS release_track_artist_idx_release ON release_track_artist (release_id);
CREATE INDEX IF NOT EXISTS release_track_artist_idx_track_id ON release_track_artist (track_id);
CREATE INDEX IF NOT EXISTS release_track_artist_idx_track_sequence ON release_track_artist (track_sequence);
CREATE INDEX IF NOT EXISTS release_track_artist_idx_artist ON release_track_artist (artist_id);
CREATE INDEX IF NOT EXISTS release_identifier_idx_release ON release_identifier (release_id);
CREATE INDEX IF NOT EXISTS release_video_idx_release ON release_video (release_id);
CREATE INDEX IF NOT EXISTS release_company_idx_release ON release_company (release_id);
CREATE INDEX IF NOT EXISTS release_company_idx_company ON release_company (company_id);

-- Discogs has some track titles that are too long for a regular index, so we have to use full text indexing
CREATE INDEX IF NOT EXISTS release_track_idx_ts_title ON release_track using gin(to_tsvector('english', title));
