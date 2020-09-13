# Discogs .NET Parser

This alternative `discogsxml2db` is written in C# and runs on Microsoft .NET Core,
although the latter is required only for development; builds that require
no installation are provided with each release.

It provides a significant speedup over the python version:

| File | Record Count | Python | C# |
| --- | ---: | :---: | :---: |
| discogs_20200806_artists.xml.gz  |  7,046,615 | 6:22    | 2:35 |
| discogs_20200806_labels.xml.gz   |  1,571,873 | 1:15    | 0:22 |
| discogs_20200806_masters.xml.gz  |  1,734,371 | 3:56    | 1:57 |
| discogs_20200806_releases.xml.gz | 12,867,980 | 1:45:16 | 42:38 |

## Features

**Done**:

- parsing all four discogs dumps, both *.xml* and *.xml.gz*;
- exporting to csv and compressed csv. Produces the exact same
  files that the Python version does;
- displaying progress of import/export process;
- "dry runs": only parsing the files and displaying counts,
  not producing any csv files;

**TODO**:

- option to track progress display against the most recently reported
  discogs record counts (`--api-counts` argument);
- option to import the resulting csv files into the database;
  this process is currently manual or done through the python DB-specific
  scripts;
- option to specify the output folder for csv files;

## Installing

Unlike the Python version, this version requires no installation.

Simply download [from the release page](https://github.com/philipmat/discogs-xml2db/releases)
the archive appropriate for your platform. Unzip,
and you should have 2 files: a `discogs` executable (or `discogs.exe` on
Windows) and a "discogs.pdb" support file.

That's it.

## Running

Executing `discogs` without any parameters or passing `--help` will
output a list of available arguments:

```text
Usage: discogs [options] [files...]

Options:

--dry-run   Parse the files, output counts, but don't write any actual files
--verbose   More verbose output
--gz        Compress output files (gzip)
files...    Path to discogs_[date]_[type].xml, or .xml.gz files.
            Can specify multiple files.
```

To export one or more discogs xml files to csv, simply pass it as parameters
to the executable: `discogs /tmp/discogs_20200806_artists.xml.gz /tmp/discogs_20200806_labels.xml.gz`.  

Currently, the program exports the csv files in the same folder as each of the
original xml files. If you would like the csv files to be compressed to `.csv.gz`,
pass the `--gz` argument: `discogs --gz /tmp/discogs_20200806_artists.xml.gz`.
