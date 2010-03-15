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

--
-- Name: artist; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE artist (
    name text NOT NULL,
    realname text,
    urls text[],
    namevariations text[],
    aliases text[],
    releases integer[],
    profile text,
    members text[],
    groups text[]
);


--
-- Name: artists_images; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE artists_images (
    image_uri text,
    artist_name text
);


--
-- Name: country; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE country (
    name text
);


--
-- Name: format; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE format (
    name text NOT NULL
);


--
-- Name: genre; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE genre (
    id integer NOT NULL,
    name text,
    parent_genre integer,
    sub_genre integer
);


--
-- Name: image; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE image (
    height integer,
    width integer,
    type text,
    uri text NOT NULL,
    uri150 text
);


--
-- Name: label; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE label (
    name text NOT NULL,
    contactinfo text,
    profile text,
    parent_label text,
    sublabels text[],
    urls text[]
);


--
-- Name: labels_images; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE labels_images (
    image_uri text,
    label_name text
);


--
-- Name: release; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE release (
    discogs_id integer NOT NULL,
    status text,
    title text,
    country text,
    released text,
    notes text,
    genres text,
    styles text
);


--
-- Name: releases_artists; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_artists (
    artist_name text,
    discogs_id integer
);


--
-- Name: releases_artists_joins; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_artists_joins (
    artist1 text,
    artist2 text,
    join_relation text,
    discogs_id integer
);


--
-- Name: releases_extraartists; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_extraartists (
    discogs_id integer,
    artist_name text,
    roles text[]
);


--
-- Name: releases_formats; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_formats (
    discogs_id integer,
    format_name text,
    qty integer,
    descriptions text[]
);


--
-- Name: releases_images; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_images (
    image_uri text,
    discogs_id integer
);


--
-- Name: releases_labels; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE releases_labels (
    label text,
    discogs_id integer,
    catno text
);


--
-- Name: role; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE role (
    role_name text
);


--
-- Name: track; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE track (
    discogs_id integer,
    title text,
    duration text,
    "position" text,
    track_id text
);


--
-- Name: tracks_artists; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE tracks_artists (
    artist_name text,
    track_id text
);


--
-- Name: tracks_artists_joins; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE tracks_artists_joins (
    artist1 text,
    artist2 text,
    join_relation text,
    track_id text
);


--
-- Name: tracks_extraartists; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE tracks_extraartists (
    artist_name text,
    track_id text
);


--
-- Name: tracks_extraartists_roles; Type: TABLE; Schema: public; Owner: -; Tablespace: 
--

CREATE TABLE tracks_extraartists_roles (
    track_id text,
    artist_name text,
    role_name text,
    role_details text
);


--
-- Name: artist_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY artist
    ADD CONSTRAINT artist_pkey PRIMARY KEY (name);


--
-- Name: format_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY format
    ADD CONSTRAINT format_pkey PRIMARY KEY (name);


--
-- Name: genre_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY genre
    ADD CONSTRAINT genre_pkey PRIMARY KEY (id);


--
-- Name: image_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY image
    ADD CONSTRAINT image_pkey PRIMARY KEY (uri);


--
-- Name: label_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY label
    ADD CONSTRAINT label_pkey PRIMARY KEY (name);


--
-- Name: release_pkey; Type: CONSTRAINT; Schema: public; Owner: -; Tablespace: 
--

ALTER TABLE ONLY release
    ADD CONSTRAINT release_pkey PRIMARY KEY (discogs_id);


--
-- Name: artists_images_artist_name_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY artists_images
    ADD CONSTRAINT artists_images_artist_name_fkey FOREIGN KEY (artist_name) REFERENCES artist(name);


--
-- Name: artists_images_image_uri_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY artists_images
    ADD CONSTRAINT artists_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);


--
-- Name: foreign_did; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY releases_labels
    ADD CONSTRAINT foreign_did FOREIGN KEY (discogs_id) REFERENCES release(discogs_id);


--
-- Name: labels_images_image_uri_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY labels_images
    ADD CONSTRAINT labels_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);


--
-- Name: labels_images_label_name_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY labels_images
    ADD CONSTRAINT labels_images_label_name_fkey FOREIGN KEY (label_name) REFERENCES label(name);


--
-- Name: releases_formats_discogs_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY releases_formats
    ADD CONSTRAINT releases_formats_discogs_id_fkey FOREIGN KEY (discogs_id) REFERENCES release(discogs_id);


--
-- Name: releases_formats_format_name_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY releases_formats
    ADD CONSTRAINT releases_formats_format_name_fkey FOREIGN KEY (format_name) REFERENCES format(name);


--
-- Name: releases_images_discogs_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY releases_images
    ADD CONSTRAINT releases_images_discogs_id_fkey FOREIGN KEY (discogs_id) REFERENCES release(discogs_id);


--
-- Name: releases_images_image_uri_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY releases_images
    ADD CONSTRAINT releases_images_image_uri_fkey FOREIGN KEY (image_uri) REFERENCES image(uri);


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

