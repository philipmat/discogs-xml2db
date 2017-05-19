
import exporters.jsonexporter


exporters = {
    'json': 'exporters.jsonexporter.JsonConsoleExporter',
    'pgsql': 'exporters.postgresexporter.PostgresExporter',
    'pgdump': 'exporters.postgresexporter.PostgresConsoleDumper',
    'couch': 'exporters.couchdbexporter.CouchDbExporter',
    'mongo': 'exporters.mongodbexporter.MongoDbExporter',
}


def _select_exporter(options):
    if options.output is None:
        return exporters['json']

    if options.output in exporters:
        return exporters[options.output]

    return options.output


def make_exporter(options):
    exp_module = _select_exporter(options)

    parts = exp_module.split('.')
    m = __import__('.'.join(parts[:-1]))
    for p in parts[1:]:
        m = getattr(m, p)

    data_quality = list(x.strip().lower() for x in (options.data_quality or '').split(',') if x)
    return m(options.params, data_quality=data_quality)
