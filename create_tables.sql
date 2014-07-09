SET client_encoding = 'UTF8';
SET standard_conforming_strings = off;
SET check_function_bodies = false;
SET client_min_messages = warning;
SET escape_string_warning = off;

SET search_path = public, pg_catalog;

SET default_tablespace = '';

SET default_with_oids = false;

SET synchronous_commit=off;

CREATE UNLOGGED TABLE artist (
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

CREATE UNLOGGED TABLE tmp_artists_images (
    image_uri 	text,
    type 		text,
    artist_id 	integer
);


CREATE UNLOGGED TABLE artists_images (
    image_uri 	text,
    type 		text,
    artist_id 	integer
);

CREATE UNLOGGED TABLE country (
    name text
);

CREATE UNLOGGED TABLE format (
    name text NOT NULL
);

CREATE UNLOGGED TABLE genre (
    id 				integer NOT NULL,
    name 			text,
    parent_genre 	integer,
    sub_genre 		integer
);

CREATE UNLOGGED TABLE image (
    height 			integer,
    width 			integer,
    uri 			text NOT NULL,
    uri150 			text
);

CREATE UNLOGGED TABLE tmp_image (
    height 			integer,
    width 			integer,
    uri 			text NOT NULL,
    uri150 			text
);


CREATE UNLOGGED TABLE label (
    id 				integer NOT NULL,
    name 			text NOT NULL,
    contactinfo 	text,
    profile 		text,
    parent_label 	text,
    sublabels 		text[],
    urls 			text[],
	data_quality 	text
);

CREATE UNLOGGED TABLE tmp_labels_images (
    image_uri 	text,
    type 		text,
    label_id 	integer
);

CREATE UNLOGGED TABLE labels_images (
    image_uri 	text,
    type 		text,
    label_id 	integer
);


CREATE UNLOGGED TABLE release (
    id 			integer NOT NULL,
    status 		text,
    title 		text,
    country 	text,
    released 	text,
	barcode		text,
    notes 		text,
    genres 		text,
    styles 		text,
    master_id 	int,
	data_quality text
);

CREATE UNLOGGED TABLE releases_artists (
    release_id 		integer,
    "position"  	integer,
    artist_id 		integer,
    artist_name 	text,
    anv 			text,
    join_relation 	text
);

CREATE UNLOGGED TABLE releases_extraartists (
    release_id 		integer,
    artist_id 		integer,
    artist_name 	text,
    anv 			text,
    role 			text
);

CREATE UNLOGGED TABLE releases_formats (
    release_id 		integer,
    "position" 		integer,
    format_name 	text,
    qty 			integer,
    descriptions 	text[]
);

CREATE UNLOGGED TABLE tmp_releases_images (
    image_uri 	text,
    type 		text,
    release_id 	integer
);

CREATE UNLOGGED TABLE releases_images (
    image_uri 	text,
    type 		text,
    release_id 	integer
);


CREATE UNLOGGED TABLE releases_labels (
    label 		text,
    release_id 	integer,
    catno 		text
);

CREATE UNLOGGED TABLE role (
    role_name text
);

CREATE UNLOGGED TABLE track (
    release_id 	integer,
    "position" 	text,
    track_id 	text,
    title 		text,
    duration 	text,
    trackno 	integer
);

CREATE UNLOGGED TABLE tracks_artists (
    track_id 		text,
    "position"  	integer,
    artist_id 		integer,
    artist_name 	text,
    anv		 		text,
    join_relation 	text
);

CREATE UNLOGGED TABLE tracks_extraartists (
    track_id 	text,
    artist_id 	integer,
    artist_name text,
    anv		 	text,
    role 		text
);

CREATE UNLOGGED TABLE master (
    id 				integer NOT NULL,
    title 			text,
    main_release 	integer NOT NULL,
    year 			int,
    notes 			text,
    genres 			text,
    styles 			text,
	data_quality 	text
);

CREATE UNLOGGED TABLE masters_artists (
    artist_name text,
    master_id integer
);

CREATE UNLOGGED TABLE masters_artists_joins (
    artist1 		text,
    artist2 		text,
    join_relation 	text,
    master_id 		integer
);

CREATE UNLOGGED TABLE masters_extraartists (
    master_id 		integer,
    artist_name 	text,
    roles 			text[]
);

CREATE UNLOGGED TABLE masters_formats (
    master_id 		integer,
    format_name 	text,
    qty 			integer,
    descriptions 	text[]
);

CREATE UNLOGGED TABLE tmp_masters_images (
    image_uri text,
    type 		text,
    master_id integer
);

CREATE UNLOGGED TABLE masters_images (
    image_uri text,
    type 		text,
    master_id integer
);

REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA public FROM postgres;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO PUBLIC;
