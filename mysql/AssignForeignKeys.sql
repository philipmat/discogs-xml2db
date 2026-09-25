-- Warning: Foreign keys take a long time to create on mysql!
-- This file will take quite a while. Grab some coffee.

-- artist_id foreign keys
ALTER TABLE artist_alias ADD CONSTRAINT artist_alias__artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE artist_image ADD CONSTRAINT artist_image__artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE artist_namevariation ADD CONSTRAINT artist_namevariation__artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE artist_url ADD CONSTRAINT artist_url__artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE group_member ADD CONSTRAINT group_member__artist_fk FOREIGN KEY (member_artist_id) REFERENCES `artist` (id);
ALTER TABLE release_artist ADD CONSTRAINT release_artist_artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE master_artist ADD CONSTRAINT master_artist_artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);
ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist__artist_fk FOREIGN KEY (artist_id) REFERENCES `artist` (id);

-- label_id foreign keys
ALTER TABLE label_image ADD CONSTRAINT label_image__label_fk FOREIGN KEY (label_id) REFERENCES `label` (id);
ALTER TABLE label_url ADD CONSTRAINT label_url__label_fk FOREIGN KEY (label_id) REFERENCES `label` (id);
ALTER TABLE release_label ADD CONSTRAINT release_label__label_fk FOREIGN KEY (label_id) REFERENCES `label` (id);

-- master_id foreign keys
ALTER TABLE `release` ADD CONSTRAINT release__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);
ALTER TABLE master_artist ADD CONSTRAINT master_artist__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);
ALTER TABLE master_genre ADD CONSTRAINT master_genre__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);
ALTER TABLE master_image ADD CONSTRAINT master_image__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);
ALTER TABLE master_style ADD CONSTRAINT master_style__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);
ALTER TABLE master_video ADD CONSTRAINT master_video__master_fk FOREIGN KEY (master_id) REFERENCES `master` (id);

-- release_id foreign keys
ALTER TABLE `master` ADD CONSTRAINT master__main_release_fk FOREIGN KEY (main_release) REFERENCES `release` (id);
ALTER TABLE release_artist ADD CONSTRAINT release_artist__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_company ADD CONSTRAINT release_company__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_format ADD CONSTRAINT release_format__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_genre ADD CONSTRAINT release_genre__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_identifier ADD CONSTRAINT release_identifier__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_image ADD CONSTRAINT release_image__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_label ADD CONSTRAINT release_label__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_style ADD CONSTRAINT release_style__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_track ADD CONSTRAINT release_track__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);
ALTER TABLE release_video ADD CONSTRAINT release_video__release_fk FOREIGN KEY (release_id) REFERENCES `release` (id);

-- track_id foreign keys
# ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist__track_fk FOREIGN KEY (track_id) REFERENCES `release_track_artist` (id);
