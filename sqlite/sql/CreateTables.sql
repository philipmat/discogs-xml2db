-- artists
CREATE TABLE artist (
    id              INTEGER PRIMARY KEY,
    name            TEXT,
    realname        TEXT,
    profile         TEXT,

    data_quality    TEXT
);

CREATE TABLE artist_url (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    artist_id       INTEGER NOT NULL,
    url             TEXT
);

CREATE TABLE artist_namevariation (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    artist_id       INTEGER NOT NULL,
    name            TEXT NOT NULL
);

CREATE TABLE artist_alias (
    artist_id       INTEGER NOT NULL,
    alias_name      TEXT NOT NULL,
    alias_artist_id INTEGER
);

CREATE TABLE artist_image (
    artist_id       INTEGER NOT NULL,
    type            TEXT,
    width           INTEGER,
    height          INTEGER
);

CREATE TABLE group_member (
    group_artist_id     INTEGER NOT NULL,
    member_artist_id    INTEGER NOT NULL,
    member_name         TEXT NOT NULL
);

-- labels
CREATE TABLE label (
    id              INTEGER PRIMARY KEY,
    name            TEXT NOT NULL,
    contact_info    TEXT,
    profile         TEXT,
    parent_id       INTEGER,
    parent_name     TEXT,
    data_quality    TEXT
);

CREATE TABLE label_url (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    label_id        INTEGER NOT NULL,
    url             TEXT NOT NULL
);

CREATE TABLE label_image (
    label_id        INTEGER NOT NULL,
    type            TEXT,
    width           INTEGER,
    height          INTEGER
);

-- masters
CREATE TABLE master (
    id              INTEGER PRIMARY KEY,
    title           TEXT NOT NULL,
    year            INTEGER,
    main_release    INTEGER NOT NULL,
    data_quality    TEXT
);

CREATE TABLE master_artist (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    master_id       INTEGER NOT NULL,
    artist_id       INTEGER NOT NULL,
    artist_name     TEXT,
    anv             TEXT,
    position        INTEGER,
    join_string     TEXT,
    role            TEXT
);

CREATE TABLE master_video (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    master_id       INTEGER NOT NULL,
    duration        INTEGER,
    title           TEXT,
    description     TEXT,
    uri             TEXT
);

CREATE TABLE master_genre (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    master_id       INTEGER NOT NULL,
    genre           TEXT
);

CREATE TABLE master_style (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    master_id       INTEGER NOT NULL,
    style           TEXT
);

CREATE TABLE master_image (
    master_id       INTEGER NOT NULL,
    type            TEXT,
    width           INTEGER,
    height          INTEGER
);

-- releases
CREATE TABLE release (
    id              INTEGER PRIMARY KEY,
    title           TEXT NOT NULL,
    released        TEXT,
    country         TEXT,
    notes           TEXT,
    data_quality    TEXT,
    main            INTEGER,
    master_id       INTEGER,
    status          TEXT
);

CREATE TABLE release_artist (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    artist_id       INTEGER NOT NULL,
    artist_name     TEXT,
    extra           INTEGER NOT NULL,
    anv             TEXT,
    position        INTEGER,
    join_string     TEXT,
    role            TEXT,
    tracks          TEXT
);

CREATE TABLE release_label (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    label_id        INTEGER,
    label_name      TEXT NOT NULL,
    catno           TEXT
);

CREATE TABLE release_genre (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    genre           TEXT
);

CREATE TABLE release_style (
    release_id      INTEGER NOT NULL,
    style           TEXT
);

CREATE TABLE release_format (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    name            TEXT,
    qty             NUMERIC,
    text_string     TEXT,
    descriptions    TEXT
);

CREATE TABLE release_track (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    sequence        INTEGER NOT NULL,
    position        TEXT,
    parent          TEXT,
    title           TEXT,
    duration        TEXT,
    track_id        TEXT
);

CREATE TABLE release_track_artist (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    track_id        TEXT,
    release_id      INTEGER NOT NULL,
    track_sequence  TEXT,
    artist_id       INTEGER NOT NULL,
    artist_name     TEXT,
    extra           INTEGER NOT NULL,
    anv             TEXT,
    position        INTEGER,
    join_string     TEXT,
    role            TEXT,
    tracks          TEXT
);

CREATE TABLE release_identifier (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    description     TEXT,
    type            TEXT,
    value           TEXT
);

CREATE TABLE release_video (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id      INTEGER NOT NULL,
    duration        INTEGER,
    title           TEXT,
    description     TEXT,
    uri             TEXT
);

CREATE TABLE release_company (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    release_id          INTEGER NOT NULL,
    company_id          INTEGER NOT NULL,
    company_name        TEXT NOT NULL,
    entity_type         TEXT,
    entity_type_name    TEXT,
    uri                 TEXT
);

CREATE TABLE release_image (
    release_id      INTEGER NOT NULL,
    type            TEXT,
    width           INTEGER,
    height          INTEGER
);
