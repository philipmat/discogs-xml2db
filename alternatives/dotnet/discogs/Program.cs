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
                Console.WriteLine($"Variant2: {fileName}");
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
            else if (Path.GetFileName(fileName) == "label.xml")
            {
                DeserializeOneToJson<discogs.Labels.label>(fileName);
            }
            else if (Path.GetFileName(fileName) == "release.xml")
            {
                DeserializeOneToJson<discogs.Releases.release>(fileName);
            }
            else if (Path.GetFileName(fileName) == "master.xml")
            {
                DeserializeOneToJson<discogs.Masters.master>(fileName);
            }
            else if (Path.GetFileName(fileName) == "artist.xml")
            {
                DeserializeOneToJson<discogs.Artists.artist>(fileName);
            }
            else if (fileName == "serialize-label")
            {
                SerializeLabel();
            }
        }

        private static void DeserializeOneToJson<T>(string fileName)
            where T : IExportToCsv, new()
        {
            using var reader = new StreamReader(fileName);

            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(T));
            var obj = (T)_labelXmlSerializer.Deserialize(reader);
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            var objJson = JsonSerializer.Serialize(obj, jsonOptions);
            Console.WriteLine($@"JSON {typeof(T).Name}:
 {objJson}");
        }

        private static void SerializeLabel()
        {
            var label = new discogs.Labels.label
            {
                images = new[] {
                    new discogs.image { type = "primary", uri="", uri150="", width="132", height="24"}
                },
                contactinfo = @"Planet
                E
                Communication",
                parentLabel = new Labels.parentLabel { id = "123", name = "The Parent Label" },
                data_quality = "Correct",
                id = "1",
                name = "Planet E",
                urls = new[] {
                    // new discogs.url { TheUrl = "http://planet-e.net" }
                     "http://planet-e.net"
                },
                sublabels = new[] {
                    new Labels.label { SubId = "2", SubName = "Antidote (4)"}
                }
            };
            var xml = new XmlSerializer(typeof(Labels.label));

            using var writer = new StringWriter();
            var xmlSettings = new XmlWriterSettings
            {
                Indent = true,
            };
            using var xmlWriter = XmlWriter.Create(writer);
            xml.Serialize(writer, label);
            var labelXml = writer.ToString();
            Console.WriteLine($@"XML label:
{labelXml}");
        }

        private static async Task ParseAsync<T>(string fileName) 
            where T : IExportToCsv, new()
        {
            var typeName = typeof(T).Name.Split('.')[^1];
            const int throttle = 1_000;
            var ticks = Statistics[typeName] / 1000;
            var pbarOptions = new ShellProgressBar.ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                ShowEstimatedDuration = true,
                CollapseWhenFinished = true,
            };
            using var pbar = new ShellProgressBar.ProgressBar(ticks, $"Parsing {typeName}s");
            using var exporter = new CsvExporter<T>(Path.GetDirectoryName(fileName));
            var parser = new Parser<T>(exporter, throttle);
            parser.OnSucessfulParse += (o, e) => pbar.Tick();
            await parser.ParseFileAsync(fileName);
        }
    }
}