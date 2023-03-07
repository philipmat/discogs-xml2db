--- artists
TRUNCATE TABLE artist;
TRUNCATE TABLE artist_url;
TRUNCATE TABLE artist_namevariation;
TRUNCATE TABLE artist_alias;
TRUNCATE TABLE artist_image;
TRUNCATE TABLE group_member;

--- labels
TRUNCATE TABLE label;
TRUNCATE TABLE label_url;
TRUNCATE TABLE label_image;

--- masters
TRUNCATE TABLE master;
TRUNCATE TABLE master_artist;
TRUNCATE TABLE master_video;
TRUNCATE TABLE master_genre;
TRUNCATE TABLE master_style;
TRUNCATE TABLE master_image;

--- releases
TRUNCATE TABLE release;
TRUNCATE TABLE release_artist;
TRUNCATE TABLE release_format;
TRUNCATE TABLE release_label;
TRUNCATE TABLE release_genre;
TRUNCATE TABLE release_style;
TRUNCATE TABLE release_track;
TRUNCATE TABLE release_track_artist;
TRUNCATE TABLE release_identifier;
TRUNCATE TABLE release_video;
TRUNCATE TABLE release_company;
TRUNCATE TABLE release_image;
