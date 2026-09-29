# Discogs .NET Parser

This alternative `discogsxml2db` is written in C# and targets .NET 10.
The .NET 10 SDK is required for development; builds that require no installation
are provided with each release.

It provides a significant speedup over the python version:

| File                             | Record Count | Python  |  C#   |
|----------------------------------|-------------:|:-------:|:-----:|
| discogs_20260901_artists.xml.gz  |   10,203,002 |  12:20  | 1:40  |
| discogs_20200901_labels.xml.gz   |    2,415,475 |  2:37   | 0:19  |
| discogs_20200901_masters.xml.gz  |    2,589,349 |  9:12   | 2:04  |
| discogs_20200901_releases.xml.gz |   19,417,067 | 3:11:42 | 50:04 |

## Features

**Done**:

- parsing all four discogs dumps, both *.xml* and *.xml.gz*;
- exporting to csv and compressed csv. Produces the exact same
  files that the Python version does;
- displaying progress of import/export process;
- "dry runs": only parsing the files and displaying counts,
  not producing any csv files;
- specifying the output folder for csv files (`--output`);

**TODO**:

- option to track progress display against the most recently reported
  discogs record counts (`--api-counts` argument);
- option to import the resulting csv files into the database;
  this process is currently manual or done through the python DB-specific
  scripts;

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
Usage:
  discogs <files>... [options]

Arguments:
  <files>  Path to discogs_[date]_[type].xml, or .xml.gz files. Can specify multiple files.

Options:
  --dry-run           Parse the files, output counts, but don't write any actual files.
  --verbose           More verbose output.
  --gz                Compress output files (gzip).
  -o, --output <dir>  Where to write the csv files. Defaults to the current directory.
  -?, -h, --help      Show help and usage information
  --version           Show version information
```

To export one or more discogs xml files to csv, pass them as arguments:
`discogs /tmp/discogs_20200806_artists.xml.gz /tmp/discogs_20200806_labels.xml.gz`.

The csv files are written to the current directory, unless a different folder
is specified with `--output` (the folder is created if it doesn't exist).
If you would like the csv files to be compressed to `.csv.gz`, pass the `--gz` argument.

### Using the provided binaries

After downloading and unzipping the release for your platform:

```bash
# Linux / macOS
./discogs --gz --output /tmp/csv /tmp/discogs_20200806_artists.xml.gz /tmp/discogs_20200806_labels.xml.gz
```

```powershell
# Windows
.\discogs.exe --gz --output C:\discogs\csv C:\discogs\discogs_20200806_artists.xml.gz
```

### Running from source with `dotnet run --project`

Requires the .NET 10 SDK. From the `alternatives/dotnet` folder,
everything after `--` is passed to the program:

```bash
dotnet run --project discogs -c Release -- --gz --output /tmp/csv /tmp/discogs_20200806_artists.xml.gz
```

### Running a local build

Build once, then run the resulting executable (or the `.dll` through `dotnet`):

```bash
dotnet build discogs -c Release
./discogs/bin/Release/net10.0/discogs --output /tmp/csv /tmp/discogs_20200806_artists.xml.gz
dotnet discogs/bin/Release/net10.0/discogs.dll --output /tmp/csv /tmp/discogs_20200806_artists.xml.gz
```

On Windows the executable is `discogs\bin\Release\net10.0\discogs.exe`.

To produce a self-contained, single-file executable like the ones in the releases:

```bash
dotnet publish discogs/discogs.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./artifacts/discogs-linux
./artifacts/discogs-linux/discogs --help
```

Use `-r osx-x64` or `-r win-x64` for other platforms.

## Generating XML fixtures

To generate sample XML files for parity testing,
see the `Generating XML fixtures` section in the [Python README](../python/README.md).
