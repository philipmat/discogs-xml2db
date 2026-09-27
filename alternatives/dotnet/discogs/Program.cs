using System.CommandLine;
using System.CommandLine.Help;
using ShellProgressBar;

namespace discogs;

public static class Program
{
    private const int ExitOk = 0;
    private const int ExitHelp = 1;
    private const int ExitParamIssue = 2;
    private const int ProgressDisplayThrottle = 1_000; // display only once every "ProgressDisplayThrottle" increment

    private static readonly Dictionary<string, int> _statistics = new([], StringComparer.OrdinalIgnoreCase)
    {
        ["release"] = 20_000_000,
        ["artist"] = 10_000_000,
        ["label"] = 2_500_000,
        ["master"] = 2_600_000
    };

    static async Task<int> Main(string[] args)
    {
        if (!TryParseArguments(args, out RunOptions parsedOptions, out List<string> files, out int exitCode))
        {
            return exitCode;
        }

        using RunOptions options = parsedOptions;
        if (!options.DryRun)
        {
            Directory.CreateDirectory(options.OutputDirectory);
        }

        List<Task> tasks = files.Select(f => ParseFile(f, options)).ToList();
        await Task.WhenAll(tasks);
        return ExitOk;
    }

    private static bool TryParseArguments(
        string[] args,
        out RunOptions options,
        out List<string> files,
        out int exitCode)
    {
        Option<bool> dryRunOption = new("--dry-run")
        {
            Description = "Parse the files, output counts, but don't write any actual files."
        };
        Option<bool> verboseOption = new("--verbose")
        {
            Description = "More verbose output."
        };
        Option<bool> gzOption = new("--gz")
        {
            Description = "Compress output files (gzip)."
        };
        Option<DirectoryInfo> outputOption = new("--output", "-o")
        {
            Description = "Where to write the csv files. Defaults to the current directory.",
            HelpName = "dir",
        };
        Option<bool> v1Option = new("--v1")
        {
            Description = "Use the older, XmlSerializer-based parser."
        };
        Argument<FileInfo[]> filesArgument = new("files")
        {
            Description = "Path to discogs_[date]_[type].xml, or .xml.gz files. Can specify multiple files.",
            Arity = ArgumentArity.OneOrMore,
        };
        filesArgument.AcceptExistingOnly();

        RootCommand rootCommand = new("Converts discogs XML files for database import.")
        {
            dryRunOption,
            verboseOption,
            gzOption,
            outputOption,
            v1Option,
            filesArgument,
        };

        ParseResult parseResult = rootCommand.Parse(args.Length == 0 ? ["--help"] : args);
        options = null;
        files = [];

        if (parseResult.Errors.Count > 0)
        {
            // prints the errors, followed by the usage
            parseResult.Invoke();
            exitCode = ExitParamIssue;
            return false;
        }

        if (parseResult.Action is not null)
        {
            // --help or --version
            parseResult.Invoke();
            exitCode = parseResult.Action is HelpAction ? ExitHelp : ExitOk;
            return false;
        }

        files = parseResult.GetRequiredValue(filesArgument).Select(f => f.FullName).ToList();
        options = new RunOptions
        {
            DryRun = parseResult.GetValue(dryRunOption),
            Verbose = parseResult.GetValue(verboseOption),
            CompressOutput = parseResult.GetValue(gzOption),
            UseVersion1 = parseResult.GetValue(v1Option),
            OutputDirectory = parseResult.GetValue(outputOption)?.FullName ?? Directory.GetCurrentDirectory(),
            FileCount = files.Count,
        };
        exitCode = ExitOk;
        return true;
    }

    private static async Task ParseFile(string fileName, RunOptions options)
    {
        fileName = Path.GetFullPath(fileName);
        if (fileName.Contains("_labels"))
        {
            await ParseAsync<Label>(fileName, options);
        }
        else if (fileName.Contains("_releases"))
        {
            await ParseAsync<Release>(fileName, options);
        }
        else if (fileName.Contains("_artists"))
        {
            await ParseAsync<Artist>(fileName, options);
        }
        else if (fileName.Contains("_masters"))
        {
            await ParseAsync<Master>(fileName, options);
        }
    }

    private static async Task ParseAsync<T>(string fileName, RunOptions options)
        where T : IExportable, new()
    {
        string typeName = typeof(T).Name.Split('.')[^1];
        int ticks = _statistics[typeName] / 1000;
        IExporter<T> exporter;
        if (options.DryRun)
        {
            exporter = new RecordCounter<T>(options.Verbose);
        }
        else
        {
            exporter = new CsvExporter<T>(
                options.OutputDirectory,
                compress: options.CompressOutput,
                verbose: options.Verbose);
        }

        ProgressBarBase progBar = options.GetProgress(typeName, ticks);

        Parser<T> parser = options.UseVersion1
            ? new XmlSerializerBasedParser<T>(exporter, ProgressDisplayThrottle)
            : new Parser<T>(exporter, ProgressDisplayThrottle);
        parser.OnSucessfulParse += (_, _) => progBar.Tick();
        await parser.ParseFileAsync(fileName);
        exporter.Dispose();
        options.Finished(progBar);
    }

    private class RunOptions : IDisposable
    {
        public bool Verbose;
        public bool DryRun;
        public bool CompressOutput;

        public bool UseVersion1;

        public string OutputDirectory = Directory.GetCurrentDirectory();

        public int FileCount;

        private readonly List<ProgressBarBase> _progressBars = [];
        private readonly Lock _lock = new();

        public void Dispose()
        {
            Parallel.ForEach(
                _progressBars,
                p =>
                {
                    if (p is IDisposable pd) pd.Dispose();
                });
        }

        public ProgressBarBase GetProgress(string typeName, int ticks)
        {
            if (FileCount <= 1)
            {
                ProgressBarOptions pbarOptions = new()
                {
                    DisplayTimeInRealTime = false,
                    ShowEstimatedDuration = true,
                    CollapseWhenFinished = true,
                };
                ProgressBar pbar = new(ticks, $"Parsing {typeName}s", pbarOptions);
                _progressBars.Add(pbar);
                return pbar;
            }
            else
            {
                lock (_lock)
                {
                    ProgressBar mainBar;
                    if (_progressBars.Count == 0)
                    {
                        ProgressBarOptions mainPbarOptions = new()
                        {
                            DisplayTimeInRealTime = false,
                            ShowEstimatedDuration = true,
                            CollapseWhenFinished = true,
                        };
                        mainBar = new(
                            FileCount,
                            $"Parsing {FileCount} files",
                            mainPbarOptions);
                        _progressBars.Add(mainBar);
                    }
                    else
                    {
                        mainBar = (ProgressBar)_progressBars[0];
                    }

                    ProgressBarOptions childPbarOptions = new()
                    {
                        DisplayTimeInRealTime = false,
                        ShowEstimatedDuration = true,
                        CollapseWhenFinished = false,
                        ForegroundColor = ConsoleColor.Cyan,
                    };
                    ChildProgressBar childPbar = mainBar.Spawn(ticks, $"Parsing {typeName}s", childPbarOptions);
                    _progressBars.Add(childPbar);
                    return childPbar;
                }
            }
        }

        public void Finished(ProgressBarBase pbar)
        {
            switch (pbar)
            {
                case ChildProgressBar childBar:
                    childBar.Dispose();
                    _progressBars[0].Tick();
                    break;
                case ProgressBar mainBar:
                    mainBar.Dispose();
                    break;
            }

            _progressBars.Remove(pbar);
        }
    }
}
