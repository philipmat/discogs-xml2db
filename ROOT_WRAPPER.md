# XML Root Wrapper Plan

- Introduce a shared helper (e.g. `xml_utils.ensure_root_wrapper(path, expected_tag)`) that opens XML/GZip inputs, peeks at the first non-declaration token to confirm the expected root start tag, peeks at the tail for the matching end tag (using a `SpooledTemporaryFile` so huge dumps spill to disk), and returns a rewound stream plus booleans indicating which boundaries were missing.
- Define a small enum (e.g. `DumpEntity`) for `ARTIST`, `LABEL`, `MASTER`, and `RELEASE` so each caller derives the expected root element and any dataset-specific handling from a single source of truth.
- When the root open tag is absent, stream a synthetic `<expected_tag>` header before the original bytes; when the closing tag is missing, append `</expected_tag>` after the payload via a lightweight iterator wrapper so we never buffer the entire dump in memory.
- Thread this helper into both entry points: `discogsparser.parseEntities` (legacy SAX path) and `speedup/exporter.EntityCsvExporter.export_from_file` so `parser.parse(...)` always receives a validated, rewound stream regardless of compression.
- Surface clear logging when we patch a file (include the file name and which boundary was synthesized) and keep behavior configurable via a new CLI flag/env override in case advanced users prefer strict failures.
- Add pytest coverage that feeds stripped-down XML snippets lacking the root open tag, the close tag, or both through the SAX and lxml flows to ensure we now parse successfully and the synthetic wrapping is exercised.
