--- artists
ALTER TABLE artist_url ADD CONSTRAINT artist_url_fk_artist FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE artist_namevariation ADD CONSTRAINT artist_namevariation_fk_artist FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE artist_alias ADD CONSTRAINT artist_alias_fk_artist FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE artist_alias ADD CONSTRAINT artist_alias_fk_alias_artist FOREIGN KEY (alias_artist_id) REFERENCES artist(id);
ALTER TABLE group_member ADD CONSTRAINT group_member_fk_group FOREIGN KEY (group_artist_id) REFERENCES artist(id);


--- labels
ALTER TABLE label ADD CONSTRAINT label_fk_parent_label FOREIGN KEY (parent_id) REFERENCES label(id);
ALTER TABLE label_url ADD CONSTRAINT label_url_fk_label FOREIGN KEY (label_id) REFERENCES label(id);


--- masters
ALTER TABLE master_artist ADD CONSTRAINT master_artist_fk_master FOREIGN KEY (master_id) REFERENCES master(id);
ALTER TABLE master_video ADD CONSTRAINT master_video_fk_master FOREIGN KEY (master_id) REFERENCES master(id);
ALTER TABLE master_genre ADD CONSTRAINT master_genre_fk_master FOREIGN KEY (master_id) REFERENCES master(id);
ALTER TABLE master_style ADD CONSTRAINT master_style_fk_master FOREIGN KEY (master_id) REFERENCES master(id);


--- releases
ALTER TABLE release_artist ADD CONSTRAINT release_artist_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_artist ADD CONSTRAINT release_artist_fk_artist FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE release_label ADD CONSTRAINT release_label_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_label ADD CONSTRAINT release_label_fk_label FOREIGN KEY (label_id) REFERENCES label(id);
ALTER TABLE release_genre ADD CONSTRAINT release_genre_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_style ADD CONSTRAINT release_style_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_format ADD CONSTRAINT release_format_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_track ADD CONSTRAINT release_track_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist_fk_artist FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE release_identifier ADD CONSTRAINT release_identifier_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_video ADD CONSTRAINT release_video_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_company ADD CONSTRAINT release_company_fk_release FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE release_company ADD CONSTRAINT release_company_fk_company FOREIGN KEY (company_id) REFERENCES label(id);
