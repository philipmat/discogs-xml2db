--
-- PostgreSQL database dump
--

SET client_encoding = 'UTF8';
SET standard_conforming_strings = off;
SET check_function_bodies = false;
SET client_min_messages = warning;
SET escape_string_warning = off;

SET search_path = public, pg_catalog;

SET default_tablespace = '';

SET default_with_oids = false;

CREATE TABLE artist (
    id 				integer NOT NULL,
    name 			text NOT NULL,
    realname 		text,
    urls 			text[],
    namevariations 	text[],
    aliases 		text[],
    releases 		integer[],
    profile 		text,
    members 		text[],
    groups 			text[],
	data_quality 	text
);


CREATE TABLE artists_images (
    image_uri 	text,
    type 		text,
    artist_id 	integer
);

CREATE TABLE country (
    name text
);

CREATE TABLE format (
    name text NOT NULL
);

CREATE TABLE genre (
    id 				integer NOT NULL,
    name 			text,
    parent_genre 	integer,
    sub_genre 		integer
);

CREATE TABLE image (
    height 			integer,
    width 			integer,
    type 			text,
    uri 			text NOT NULL,
    uri150 			text
);

CREATE TABLE label (
    id 				integer NOT NULL,
    name 			text NOT NULL,
    contactinfo 	text,
    profile 		text,
    parent_label 	text,
    sublabels 		text[],
    urls 			text[],
	data_quality 	text
);

CREATE TABLE labels_images (
    image_uri 	text,
    type 		text,
    label_id 	integer
);

CREATE TABLE release (
    id 			integer NOT NULL,
    status 		text,
    title 		text,
    country 	text,
    released 	text,
    notes 		text,
    genres 		text,
    styles 		text,
    master_id 	int,
	data_quality text
);

CREATE TABLE releases_artists (
    release_id 		integer,
    "position"  	integer,
    artist_id 		integer,
    artist_name 	text,
    anv 			text,
    relation_join 	text
);

CREATE TABLE releases_extraartists (
    release_id 		integer,
    artist_id 		integer,
    artist_name 	text,
    role 			text
);

CREATE TABLE releases_formats (
    release_id 		integer,
    "position" 		integer,
    format_name 	text,
    qty 			integer,
    descriptions 	text[]
);

CREATE TABLE releases_images (
    image_uri 	text,
    type 		text,
    release_id 	integer
);

CREATE TABLE releases_labels (
    label 		text,
    release_id 	integer,
    catno 		text
);

CREATE TABLE role (
    role_name text
);

CREATE TABLE track (
    release_id 	integer,
    "position" 	text,
    track_id 	text,
    title 		text,
    duration 	text,
    trackno 	integer
);

CREATE TABLE tracks_artists (
    track_id 		text,
    "position"  	integer,
    artist_id 		integer,
    artist_name 	text,
    anv		 		text,
    join_relation 	text
);

CREATE TABLE tracks_extraartists (
    track_id 	text,
    artist_id 	integer,
    artist_name text,
    role 		text
);

CREATE TABLE master (
    id 				integer NOT NULL,
    title 			text,
    main_release 	integer NOT NULL,
    year 			int,
    notes 			text,
    genres 			text,
    styles 			text,
	data_quality 	text
);

CREATE TABLE masters_artists (
    artist_name text,
    master_id integer
);

CREATE TABLE masters_artists_joins (
    artist1 		text,
    artist2 		text,
    join_relation 	text,
    master_id 		integer
);

CREATE TABLE masters_extraartists (
    master_id 		integer,
    artist_name 	text,
    roles 			text[]
);

CREATE TABLE masters_formats (
    master_id 		integer,
    format_name 	text,
    qty 			integer,
    descriptions 	text[]
);

CREATE TABLE masters_images (
    image_uri text,
    type 		text,
    master_id integer
);


ALTER TABLE ONLY artist ADD CONSTRAINT artist_pkey PRIMARY KEY (id);
ALTER TABLE ONLY format ADD CONSTRAINT format_pkey PRIMARY KEY (name);
ALTER TABLE ONLY genre ADD CONSTRAINT genre_pkey PRIMARY KEY (id);
ALTER TABLE ONLY image ADD CONSTRAINT image_pkey PRIMARY KEY (uri);
ALTER TABLE ONLY label ADD CONSTRAINT label_pkey PRIMARY KEY (id);
ALTER TABLE ONLY role ADD CONSTRAINT role_pkey PRIMARY KEY (role_name);
ALTER TABLE ONLY release ADD CONSTRAINT release_pkey PRIMARY KEY (id);
ALTER TABLE ONLY track ADD CONSTRAINT track_pkey PRIMARY KEY (track_id);
ALTER TABLE ONLY master ADD CONSTRAINT master_pkey PRIMARY KEY (id);
ALTER TABLE ONLY releases_formats ADD CONSTRAINT releases_formats_pkey PRIMARY KEY (release_id, position);
ALTER TABLE ONLY releases_images ADD CONSTRAINT releases_images_pkey PRIMARY KEY (release_id, type,image_uri);
ALTER TABLE ONLY releases_artists ADD CONSTRAINT releases_artists_pkey PRIMARY KEY (release_id, position);
ALTER TABLE ONLY tracks_artists ADD CONSTRAINT tracks_artists_pkey PRIMARY KEY (track_id, position);
ALTER TABLE ONLY artists_images ADD CONSTRAINT artists_images_pkey PRIMARY KEY (artist_id, type,image_uri);
ALTER TABLE ONLY labels_images ADD CONSTRAINT labels_images_pkey PRIMARY KEY (label_id, type,image_uri);

ALTER TABLE ONLY artists_images ADD CONSTRAINT artists_images_artist_id_fkey FOREIGN KEY (artist_id) REFERENCES artist(id);
ALTER TABLE ONLY artists_images ADD CONSTRAINT artists_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);
ALTER TABLE ONLY releases_labels ADD CONSTRAINT foreign_did FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE ONLY labels_images ADD CONSTRAINT labels_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);
ALTER TABLE ONLY labels_images ADD CONSTRAINT labels_images_label_id_fkey FOREIGN KEY (label_id) REFERENCES label(id);
ALTER TABLE ONLY releases_formats ADD CONSTRAINT releases_formats_release_id_fkey FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE ONLY releases_formats ADD CONSTRAINT releases_formats_format_name_fkey FOREIGN KEY (format_name) REFERENCES format(name);
ALTER TABLE ONLY releases_images ADD CONSTRAINT releases_images_release_id_fkey FOREIGN KEY (release_id) REFERENCES release(id);
ALTER TABLE ONLY releases_images ADD CONSTRAINT releases_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);
ALTER TABLE ONLY masters_images ADD CONSTRAINT masters_images_master_id_fkey FOREIGN KEY (master_id) REFERENCES master(id);
ALTER TABLE ONLY masters_images ADD CONSTRAINT masters_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);

--
-- Name: public; Type: ACL; Schema: -; Owner: -
--

REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA public FROM postgres;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO PUBLIC;


--
-- PostgreSQL database dump complete
--

