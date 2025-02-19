--- artist
DROP TABLE IF EXISTS artist;
DROP TABLE IF EXISTS artist_alias;
DROP TABLE IF EXISTS artist_image;
DROP TABLE IF EXISTS artist_namevariation;
DROP TABLE IF EXISTS group_member;
DROP TABLE IF EXISTS artist_url;

--- labels
DROP TABLE IF EXISTS label;
DROP TABLE IF EXISTS label_image;
DROP TABLE IF EXISTS label_url;

--- masters
DROP TABLE IF EXISTS master;
DROP TABLE IF EXISTS master_artist;
DROP TABLE IF EXISTS master_genre;
DROP TABLE IF EXISTS master_image;
DROP TABLE IF EXISTS master_style;
DROP TABLE IF EXISTS master_video;

--- releases
DROP TABLE IF EXISTS release;
DROP TABLE IF EXISTS release_artist;
DROP TABLE IF EXISTS release_company;
DROP TABLE IF EXISTS release_format;
DROP TABLE IF EXISTS release_genre;
DROP TABLE IF EXISTS release_identifier;
DROP TABLE IF EXISTS release_image;
DROP TABLE IF EXISTS release_label;
DROP TABLE IF EXISTS release_style;
DROP TABLE IF EXISTS release_track;
DROP TABLE IF EXISTS release_track_artist;
DROP TABLE IF EXISTS release_video;
