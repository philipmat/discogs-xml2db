using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace discogs
{
    public class Program
    {
        private const int ExitOk = 0;
        private const int ExitHelp = 1;
        private const int ExitParamIssue = 2;
        private const int ProgressDisplayThrottle = 1_000; // display only once every ProgressDisplayThrottle
        private const string Usage = @"Converts discogs XML files for database import.
Usage: discogs [options] [files...]
 
Options:
 
--dry-run   Parse the files, output counts, but don't write any actual files
--verbose   More verbose output
--gz        Compress output files (gzip)
files...    Path to discogs_[date]_[type].xml, or .xml.gz files.
            Can specify multiple files.
 ";
        private static readonly Dictionary<string, int> Statistics = new Dictionary<string, int> {
            {"release", 12945920}, { "artist", 7075521}, { "label", 1579404}, {"master", 1250000}
        };

        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
            {
                Console.WriteLine(Usage);
                return ExitHelp;
            }

            using var options = new RunOptions();
            var files = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
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
                    Console.Error.WriteLine($"Error: Unknown argument or file {arg}.");
                    Console.WriteLine(Usage);
                    return ExitParamIssue;
                }
            }

            if (files.Count == 0)
            {
                Console.Error.WriteLine("Error: no file names passed as arguments.");
                Console.WriteLine(Usage);
                return ExitParamIssue;
            }

            options.FileCount = files.Count;
            var tasks = files.Select(f => ParseFile(f, options));
            await Task.WhenAll(tasks);
            return ExitOk;
        }


        private static async Task ParseFile(string fileName, RunOptions options)
        {
            fileName = Path.GetFullPath(fileName);
            if (fileName.Contains("_labels"))
            {
                await ParseAsync<discogs.Labels.label>(fileName, options);
            }
            else if (fileName.Contains("_releases"))
            {
                await ParseAsync<discogs.Releases.release>(fileName, options);
            }
            else if (fileName.Contains("_artists"))
            {
                await ParseAsync<discogs.Artists.artist>(fileName, options);
            }
            else if (fileName.Contains("_masters"))
            {
                await ParseAsync<discogs.Masters.master>(fileName, options);
            }
        }

        private static async Task ParseAsync<T>(string fileName, RunOptions options)
            where T : IExportable, new()
        {
            var typeName = typeof(T).Name.Split('.')[^1];
            var ticks = Statistics[typeName] / 1000;
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
            var pbar = options.GetProgress(typeName, ticks);

            Parser<T> parser = options.UseVersion1 
                ? new SerializerParser<T>(exporter, ProgressDisplayThrottle)
                : new Parser<T>(exporter, ProgressDisplayThrottle);
            parser.OnSucessfulParse += (o, e) => pbar.Tick();
            await parser.ParseFileAsync(fileName);
            exporter.Dispose();
            options.Finished(pbar);
        }

        private class RunOptions : IDisposable
        {
            public bool Verbose = false;
            public bool DryRun = false;
            public bool CompressOutput = false;

            public bool UseVersion1 = false;

            public int FileCount = 0;

            private List<ShellProgressBar.ProgressBarBase> _progressBars = new List<ShellProgressBar.ProgressBarBase>();
            private readonly object _lock = new object();

            public void Dispose()
            {
                Parallel.ForEach(_progressBars, p => { if (p is IDisposable pd) pd.Dispose(); });
            }

            public ShellProgressBar.ProgressBarBase GetProgress(string typeName, int ticks) {
                if (FileCount <= 1) {
                    var pbarOptions = new ShellProgressBar.ProgressBarOptions
                    {
                        DisplayTimeInRealTime = false,
                        ShowEstimatedDuration = true,
                        CollapseWhenFinished = true,
                    };
                    var pbar = new ShellProgressBar.ProgressBar(ticks, $"Parsing {typeName}s", pbarOptions);
                    _progressBars.Add(pbar);
                    return pbar;
                }
                else {
                    lock (_lock)
                    {
                        ShellProgressBar.ProgressBar mainBar;
                        if (_progressBars.Count == 0)
                        {
                            var mainPbarOptions = new ShellProgressBar.ProgressBarOptions
                            {
                                DisplayTimeInRealTime = false,
                                ShowEstimatedDuration = true,
                                CollapseWhenFinished = true,
                            };
                            mainBar = new ShellProgressBar.ProgressBar(FileCount, $"Parsing {FileCount} files", mainPbarOptions);
                            _progressBars.Add(mainBar);
                        }
                        else
                        {
                            mainBar = (ShellProgressBar.ProgressBar)_progressBars[0];
                        }

                        var childPbarOptions = new ShellProgressBar.ProgressBarOptions
                        {
                            DisplayTimeInRealTime = false,
                            ShowEstimatedDuration = true,
                            CollapseWhenFinished = false,
                            ForegroundColor = ConsoleColor.Cyan,
                        };
                        var childPbar = mainBar.Spawn(ticks, $"Parsing {typeName}s", childPbarOptions);
                        _progressBars.Add(childPbar);
                        return childPbar;
                    }
                }
            }

            public void Finished(ShellProgressBar.ProgressBarBase pbar) {
                if (pbar is ShellProgressBar.ChildProgressBar childBar) {
                    childBar.Dispose();
                    _progressBars[0].Tick();
                }
                else if (pbar is ShellProgressBar.ProgressBar mainBar) {
                    mainBar.Dispose();
                }
                _progressBars.Remove(pbar);
            }
        }
    }
}