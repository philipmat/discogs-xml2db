--- artists
ALTER TABLE artist ADD CONSTRAINT artist_pkey PRIMARY KEY (id);
ALTER TABLE artist_url ADD CONSTRAINT artist_url_pkey PRIMARY KEY (id);
ALTER TABLE artist_namevariation ADD CONSTRAINT artist_namevariation_pkey PRIMARY KEY (id);

--- labels
ALTER TABLE label ADD CONSTRAINT label_pkey PRIMARY KEY (id);
ALTER TABLE label_url ADD CONSTRAINT label_url_pkey PRIMARY KEY (id);

--- masters
ALTER TABLE master ADD CONSTRAINT master_pkey PRIMARY KEY (id);
ALTER TABLE master_artist ADD CONSTRAINT master_artist_pkey PRIMARY KEY (id);
ALTER TABLE master_video ADD CONSTRAINT master_video_pkey PRIMARY KEY (id);
ALTER TABLE master_genre ADD CONSTRAINT master_genre_pkey PRIMARY KEY (id);
ALTER TABLE master_style ADD CONSTRAINT master_style_pkey PRIMARY KEY (id);

--- releases
ALTER TABLE release ADD CONSTRAINT release_pkey PRIMARY KEY (id);
ALTER TABLE release_artist ADD CONSTRAINT release_artist_pkey PRIMARY KEY (id);
ALTER TABLE release_label ADD CONSTRAINT release_label_pkey PRIMARY KEY (id);
ALTER TABLE release_genre ADD CONSTRAINT release_genre_pkey PRIMARY KEY (id);
ALTER TABLE release_format ADD CONSTRAINT release_format_pkey PRIMARY KEY (id);
ALTER TABLE release_track ADD CONSTRAINT release_track_pkey PRIMARY KEY (id);
ALTER TABLE release_track_artist ADD CONSTRAINT release_track_artist_pkey PRIMARY KEY (id);
ALTER TABLE release_identifier ADD CONSTRAINT release_identifier_pkey PRIMARY KEY (id);
ALTER TABLE release_video ADD CONSTRAINT release_video_pkey PRIMARY KEY (id);
ALTER TABLE release_company ADD CONSTRAINT release_company_pkey PRIMARY KEY (id);
