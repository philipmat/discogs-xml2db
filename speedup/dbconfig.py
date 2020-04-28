import configparser
import re

import psycopg2


class ConfigSection(object):
    pass


class SchemasConfig(object):

    def __init__(self):
        self.mapping = {}

    def name(self, name):
        return self.mapping.get(name, name)

    def parse(self, parser, section):
        for name, value in parser.items(section):
            self.mapping[name] = value


class Config(object):

    def __init__(self, path):
        self.path = path
        self.cfg = configparser.RawConfigParser()
        self.cfg.read(self.path)
        self.get = self.cfg.get
        self.has_option = self.cfg.has_option
        self.database = ConfigSection()
        self.schema = SchemasConfig()
        if self.cfg.has_section('schemas'):
            self.schema.parse(self.cfg, 'schemas')

    def make_psql_args(self):
        opts = {}
        opts['database'] = self.cfg.get('DATABASE', 'name')
        opts['user'] = self.cfg.get('DATABASE', 'user')
        if self.cfg.has_option('DATABASE', 'password'):
            opts['password'] = self.cfg.get('DATABASE', 'password')
        if self.cfg.has_option('DATABASE', 'host'):
            opts['host'] = self.cfg.get('DATABASE', 'host')
        if self.cfg.has_option('DATABASE', 'port'):
            opts['port'] = self.cfg.get('DATABASE', 'port')
        return opts


def connect_db(cfg, set_search_path=False):
    db = psycopg2.connect(**cfg.make_psql_args())
    if set_search_path:
        db.cursor().execute("SET search_path TO %s", (cfg.schema.name('discogs'),))
    return db



columns = {table:columns.split() for table, columns in {
    'label':                'id name contact_info profile parent_name parent_id data_quality',
    'label_url':            'label_id url',
    'label_image':          'label_id type width height',

    'artist':               'id name realname profile data_quality',
    'artist_alias':         'artist_id alias_name',
    'artist_namevariation': 'artist_id name',
    'artist_url':           'artist_id url',
    'group_member':         'group_artist_id member_artist_id member_name',
    'artist_image':         'artist_id type width height',

    'master':               'id title year main_release data_quality',
    'master_artist':        'master_id artist_id artist_name anv position join_string role',
    'master_video':         'master_id duration title description uri',
    'master_genre':         'master_id genre',
    'master_style':         'master_id style',
    'master_image':         'master_id type width height',

    'release':              'id title released country notes data_quality master_id status',
    'release_artist':       'release_id artist_id artist_name extra anv position join_string role tracks',
    'release_label':        'release_id label_name catno',
    'release_genre':        'release_id genre',
    'release_style':        'release_id style',
    'release_format':       'release_id name qty text_string descriptions',
    'release_company':      'release_id company_id company_name entity_type entity_type_name uri',
    'release_video':        'release_id duration title description uri',
    'release_identifier':   'release_id description type value',
    'release_track':        'release_id sequence position parent title duration track_id',
    'release_track_artist': 'release_id track_sequence track_id artist_id artist_name extra anv position join_string role tracks',
    'release_image':        'release_id type width height',
}.items()}




