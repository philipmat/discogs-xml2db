-- Warning: Foreign keys take a long time to create on mysql!
-- This file will take quite a while. Grab some coffee.

-- artist_id foreign keys
ALTER TABLE artist_alias DROP FOREIGN KEY artist_alias__artist_fk;
ALTER TABLE artist_image DROP FOREIGN KEY artist_image__artist_fk;
ALTER TABLE artist_namevariation DROP FOREIGN KEY artist_namevariation__artist_fk;
ALTER TABLE artist_url DROP FOREIGN KEY artist_url__artist_fk;
ALTER TABLE group_member DROP FOREIGN KEY group_member__artist_fk;
ALTER TABLE release_artist DROP FOREIGN KEY release_artist_artist_fk;
ALTER TABLE master_artist DROP FOREIGN KEY master_artist_artist_fk;
ALTER TABLE release_track_artist DROP FOREIGN KEY release_track_artist__artist_fk;

-- label_id foreign keys
ALTER TABLE label_image DROP FOREIGN KEY label_image__label_fk;
ALTER TABLE label_url DROP FOREIGN KEY label_url__label_fk;
ALTER TABLE release_label DROP FOREIGN KEY release_label__label_fk;

-- master_id foreign keys
ALTER TABLE `release` DROP FOREIGN KEY release__master_fk;
ALTER TABLE master_artist DROP FOREIGN KEY master_artist__master_fk;
ALTER TABLE master_genre DROP FOREIGN KEY master_genre__master_fk;
ALTER TABLE master_image DROP FOREIGN KEY master_image__master_fk;
ALTER TABLE master_style DROP FOREIGN KEY master_style__master_fk;
ALTER TABLE master_video DROP FOREIGN KEY master_video__master_fk;

-- release_id foreign keys
ALTER TABLE `master` DROP FOREIGN KEY master__main_release_fk;
ALTER TABLE release_artist DROP FOREIGN KEY release_artist__release_fk;
ALTER TABLE release_company DROP FOREIGN KEY release_company__release_fk;
ALTER TABLE release_format DROP FOREIGN KEY release_format__release_fk;
ALTER TABLE release_genre DROP FOREIGN KEY release_genre__release_fk;
ALTER TABLE release_identifier DROP FOREIGN KEY release_identifier__release_fk;
ALTER TABLE release_image DROP FOREIGN KEY release_image__release_fk;
ALTER TABLE release_label DROP FOREIGN KEY release_label__release_fk;
ALTER TABLE release_style DROP FOREIGN KEY release_style__release_fk;
ALTER TABLE release_track DROP FOREIGN KEY release_track__release_fk;
ALTER TABLE release_track_artist DROP FOREIGN KEY release_track_artist__release_fk;
ALTER TABLE release_video DROP FOREIGN KEY release_video__release_fk;

-- track_id foreign keys
# ALTER TABLE release_track_artist DROP FOREIGN KEY release_track_artist__track_fk;
