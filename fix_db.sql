-- Add in missing Various artist
INSERT INTO artist(id, name)VALUES (194, 'Various');

-- Fixes multiple references to same image with out of date image sizes
insert into image
select max(t1.height), max(t1.width), t1.uri,t1.uri150
from tmp_image t1
left join image  t2
on   t1.uri 	= t2.uri
and  t1.uri150 	= t2.uri150
where t2.uri is null
group by t1.uri, t1.uri150;

--Remove duplicate artist rows
insert into artists_images
select distinct t1.image_uri, t1.type, t1.artist_id
from tmp_artists_images t1
left join artists_images t2
on   t1.image_uri 		= t2.image_uri
and  t1.type		= t2.type
and  t1.artist_id 	= t2.artist_id
where t2.image_uri is null
;

--Remove duplicate label rows
insert into labels_images
select distinct t1.image_uri, t1.type, t1.label_id
from tmp_labels_images t1
left join labels_images t2
on   t1.image_uri 		= t2.image_uri
and  t1.type		= t2.type
and  t1.label_id 	= t2.label_id
where t2.image_uri is null
;

--Remove duplicate master rows
insert into masters_images
select distinct t1.image_uri, t1.type, t1.master_id
from tmp_masters_images t1
left join masters_images t2
on   t1.image_uri 	= t2.image_uri
and  t1.type		= t2.type
and  t1.master_id 	= t2.master_id
where t2.image_uri is null
;

--Remove duplicate release rows
insert into releases_images
select distinct t1.image_uri, t1.type, t1.release_id
from tmp_releases_images t1
left join releases_images t2
on   t1.image_uri 		= t2.image_uri
and  t1.type		= t2.type
and  t1.release_id 	= t2.release_id
where t2.image_uri is null
;

-- Remove duplicate rows
CREATE TABLE TEMP AS
SELECT DISTINCT * FROM releases_labels;
DROP TABLE releases_labels;
ALTER TABLE temp RENAME TO releases_labels;


