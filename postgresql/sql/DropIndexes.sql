--- artists
DROP INDEX IF EXISTS artist_url_idx_artist;
DROP INDEX IF EXISTS artist_namevariation_idx_artist;
DROP INDEX IF EXISTS artist_alias_idx_artist;
DROP INDEX IF EXISTS group_member_idx_group;
DROP INDEX IF EXISTS group_member_idx_member;

--- labels
DROP INDEX IF EXISTS label_idx_parent_label;
DROP INDEX IF EXISTS label_url_idx_url;

--- masters
DROP INDEX IF EXISTS master_artist_idx_master;
DROP INDEX IF EXISTS master_artist_idx_artist;
DROP INDEX IF EXISTS master_video_idx_master;
DROP INDEX IF EXISTS master_genre_idx_master;
DROP INDEX IF EXISTS master_style_idx_master;

--- releases
DROP INDEX IF EXISTS release_idx_master;
DROP INDEX IF EXISTS release_artist_idx_release;
DROP INDEX IF EXISTS release_artist_idx_artist;
DROP INDEX IF EXISTS release_label_idx_release;
DROP INDEX IF EXISTS release_label_idx_label;
DROP INDEX IF EXISTS release_genre_idx_release;
DROP INDEX IF EXISTS release_style_idx_release;
DROP INDEX IF EXISTS release_format_idx_release;
DROP INDEX IF EXISTS release_track_idx_release;
DROP INDEX IF EXISTS release_track_idx_sequence;
DROP INDEX IF EXISTS release_track_idx_parent;
DROP INDEX IF EXISTS release_track_idx_title;
DROP INDEX IF EXISTS release_track_idx_ts_title;
DROP INDEX IF EXISTS release_track_uidx_track;
DROP INDEX IF EXISTS release_track_artist_idx_release;
DROP INDEX IF EXISTS release_track_artist_idx_track_id;
DROP INDEX IF EXISTS release_track_artist_idx_track_sequence;
DROP INDEX IF EXISTS release_track_artist_idx_artist;
DROP INDEX IF EXISTS release_identifier_idx_release;
DROP INDEX IF EXISTS release_video_idx_release;
DROP INDEX IF EXISTS release_company_idx_release;
DROP INDEX IF EXISTS release_company_idx_company;
