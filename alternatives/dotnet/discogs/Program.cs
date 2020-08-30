using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace discogs
{
    public class Program
    {
        private const int ProgressDisplayThrottle = 1_000; // display only once every ProgressDisplayThrottle
        private static readonly Dictionary<string, int> Statistics = new Dictionary<string, int> {
            {"release", 12945920}, { "artist", 7075521}, { "label", 1579404}, {"master", 1250000}
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args.Select((s, i) => $"{i,-2} - {s}")));
            var fileName = args[^1];
            if (Path.GetFileName(fileName).Contains("discogs"))
            {
                fileName = Path.GetFullPath(fileName);
                if (fileName.Contains("_labels"))
                {
                    await ParseAsync<discogs.Labels.label>(fileName);
                }
                else if (fileName.Contains("_releases"))
                {
                    await ParseAsync<discogs.Releases.release>(fileName);
                }
                else if (fileName.Contains("_artists"))
                {
                    await ParseAsync<discogs.Artists.artist>(fileName);
                }
                else if (fileName.Contains("_masters"))
                {
                    await ParseAsync<discogs.Masters.master>(fileName);
                }
            }
        }

        private static async Task ParseAsync<T>(string fileName) 
            where T : IExportToCsv, new()
        {
            var typeName = typeof(T).Name.Split('.')[^1];
            var ticks = Statistics[typeName] / 1000;
            var pbarOptions = new ShellProgressBar.ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                ShowEstimatedDuration = true,
                CollapseWhenFinished = true,
            };
            using var pbar = new ShellProgressBar.ProgressBar(ticks, $"Parsing {typeName}s");
            using var exporter = new CsvExporter<T>(Path.GetDirectoryName(fileName));
            var parser = new Parser<T>(exporter, ProgressDisplayThrottle);
            parser.OnSucessfulParse += (o, e) => pbar.Tick();
            await parser.ParseFileAsync(fileName);
        }
    }
}