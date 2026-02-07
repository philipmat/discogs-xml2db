using ShellProgressBar;

namespace discogs;

public class Program
{
    private const int ExitOk = 0;
    private const int ExitHelp = 1;
    private const int ExitParamIssue = 2;
    private const int ProgressDisplayThrottle = 1_000; // display only once every "ProgressDisplayThrottle" increment

    private const string Usage = """
                                 Converts discogs XML files for database import.
                                 Usage: discogs [options] [files...]

                                 Options:

                                 --dry-run   Parse the files, output counts, but don't write any actual files
                                 --verbose   More verbose output
                                 --gz        Compress output files (gzip)
                                 files...    Path to discogs_[date]_[type].xml, or .xml.gz files.
                                             Can specify multiple files.

                                 """;

    private static readonly Dictionary<string, int> _statistics = new()
    {
        ["release"] = 20_000_000,
        ["artist"] = 10_000_000,
        ["label"] = 2_500_000,
        ["master"] = 2_600_000
    };

    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Usage);
            return ExitHelp;
        }

        // TODO: use an argument parsing library
        using RunOptions options = new();
        List<string> files = [];
        foreach (string arg in args)
        {
            if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                options.DryRun = true;
            }
            else if (string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase))
            {
                options.Verbose = true;
            }
            else if (string.Equals(arg, "--gz", StringComparison.OrdinalIgnoreCase))
            {
                options.CompressOutput = true;
            }
            else if (string.Equals(arg, "--v1", StringComparison.OrdinalIgnoreCase))
            {
                options.UseVersion1 = true;
            }
            else if (File.Exists(arg))
            {
                files.Add(arg);
            }
            else
            {
                await Console.Error.WriteLineAsync($"Error: Unknown argument or file {arg}.");
                Console.WriteLine(Usage);
                return ExitParamIssue;
            }
        }

        if (files.Count == 0)
        {
            await Console.Error.WriteLineAsync("Error: no file names passed as arguments.");
            Console.WriteLine(Usage);
            return ExitParamIssue;
        }

        options.FileCount = files.Count;
        List<Task> tasks = files.Select(f => ParseFile(f, options)).ToList();
        await Task.WhenAll(tasks);
        return ExitOk;
    }


    private static async Task ParseFile(string fileName, RunOptions options)
    {
        fileName = Path.GetFullPath(fileName);
        if (fileName.Contains("_labels"))
        {
            await ParseAsync<Labels.Label>(fileName, options);
        }
        else if (fileName.Contains("_releases"))
        {
            await ParseAsync<Releases.release>(fileName, options);
        }
        else if (fileName.Contains("_artists"))
        {
            await ParseAsync<Artist>(fileName, options);
        }
        else if (fileName.Contains("_masters"))
        {
            await ParseAsync<Masters.master>(fileName, options);
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
                Path.GetDirectoryName(fileName),
                compress: options.CompressOutput,
                verbose: options.Verbose);
        }

        ProgressBarBase pbar = options.GetProgress(typeName, ticks);

        Parser<T> parser = options.UseVersion1
            ? new XmlSerializerBasedParser<T>(exporter, ProgressDisplayThrottle)
            : new Parser<T>(exporter, ProgressDisplayThrottle);
        parser.OnSucessfulParse += (_, _) => pbar.Tick();
        await parser.ParseFileAsync(fileName);
        exporter.Dispose();
        options.Finished(pbar);
    }

    private class RunOptions : IDisposable
    {
        public bool Verbose;
        public bool DryRun;
        public bool CompressOutput;

        public bool UseVersion1;

        public int FileCount;

        private readonly List<ProgressBarBase> _progressBars = new();
        private readonly object _lock = new();

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
