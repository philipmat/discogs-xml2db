-- Add in missing Various artist
INSERT INTO artist(id, name)VALUES (194, 'Various');

CREATE TABLE TEMP AS
SELECT DISTINCT ON (t1.artist_id, t1.type)  * FROM artists_images t1
ORDER BY t1.artist_id, t1.type, t1.width desc, t1.height desc;
DROP TABLE artists_images;
ALTER TABLE temp RENAME TO artists_images;

CREATE TABLE TEMP AS
SELECT DISTINCT ON (t1.label_id, t1.type)  * FROM labels_images t1
ORDER BY t1.label_id, t1.type, t1.width desc, t1.height desc;
DROP TABLE labels_images;
ALTER TABLE temp RENAME TO labels_images;

CREATE TABLE TEMP AS
SELECT DISTINCT ON (t1.master_id, t1.type)  * FROM masters_images t1
ORDER BY t1.master_id, t1.type, t1.width desc, t1.height desc;
DROP TABLE masters_images;
ALTER TABLE temp RENAME TO masters_images;


CREATE TABLE TEMP AS
SELECT DISTINCT ON (t1.release_id, t1.type)  * FROM releases_images t1
ORDER BY t1.release_id, t1.type, t1.width desc, t1.height desc;
DROP TABLE releases_images;
ALTER TABLE temp RENAME TO releases_images;

-- Remove duplicate release labels tuples
CREATE TABLE tmp_release_labels AS
SELECT DISTINCT * FROM releases_labels;
DROP TABLE releases_labels;
ALTER TABLE tmp_release_labels RENAME TO releases_labels;
