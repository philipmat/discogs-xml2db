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
                    await Variant2<discogs.Labels.label>(fileName);
                }
                else if (fileName.Contains("_releases"))
                {
                    await Variant2<discogs.Releases.release>(fileName);
                }
                else if (fileName.Contains("_artists"))
                {
                    await Variant2<discogs.Artists.artist>(fileName);
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

        private static T Deserialize<T>(string content)
        {
            using var reader = new StringReader(content);
            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(T));
            var obj = (T)_labelXmlSerializer.Deserialize(reader);
            return obj;
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

        private static Dictionary<string, (string FilePath, StreamWriter FileStream)> GetCsvFilesFor<T>(string xmlFile)
            where T : IExportToCsv, new()
        {
            var obj = new T();
            var dir = Path.GetDirectoryName(xmlFile);
            IReadOnlyDictionary<string, string[]> files = obj.GetCsvExportScheme();
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvFiles = files.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var csvFile = Path.Combine(dir, $"{kvp.Key}.csv");
                    var stream = new StreamWriter(csvFile);
                    stream.WriteLine(CsvExtensions.ToCsv(kvp.Value));
                    return (csvFile, stream);
                });

            return csvFiles;
        }

        private static async Task WriteCsvAsync<T>(
            string objectString,
            Dictionary<string, (string FilePath, StreamWriter FileStream)> streams)
            where T : IExportToCsv
        {
            var obj = Deserialize<T>(objectString);
            IEnumerable<(string StreamName, string[] Row)> csvExports = obj.ExportToCsv();
            foreach (var (streamName, row) in csvExports)
            {
                await streams[streamName].FileStream.WriteLineAsync(CsvExtensions.ToCsv(row));
            }
        }

        static async Task Variant2<T>(string fileName)
            where T : IExportToCsv, new()
        {
            var typeName = typeof(T).Name.Split('.')[^1];
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvStreams = GetCsvFilesFor<T>(fileName);
            int objectCount = 0;
            using FileStream fileStream = new FileStream(fileName, FileMode.Open);
            Stream readingStream = fileStream;
            if (System.IO.Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase))
            {
                readingStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
            };
            var ticks = Statistics[typeName] / 1000;
            var pbarOptions = new ShellProgressBar.ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                ShowEstimatedDuration = true,
                CollapseWhenFinished = true,
            };
            using var pbar = new ShellProgressBar.ProgressBar(ticks, $"Parsing {typeName}s");
            using XmlReader reader = XmlReader.Create(readingStream, settings);

            while (reader.Read())
            {
                if (reader.Name == typeName)
                {
                    var objectString = await reader.ReadOuterXmlAsync();
                    try
                    {
                        if (!string.IsNullOrEmpty(objectString))
                        {
                            await WriteCsvAsync<T>(objectString, csvStreams);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error {ex} parsing node {objectString}");
                    }
                    // var labelId = DeserializeLabel(labelString);
                    // Console.WriteLine($"label: {labelId}");
                    // Console.Write('.');
                    // _ = reader.ReadOuterXml();
                    objectCount++;
                    // await reader.SkipAsync();
                    if (objectCount % 1_000 == 0) pbar.Tick();
                    continue;
                }
            }
            var csvFileNames = string.Join("; ", csvStreams.Select(kvp => kvp.Value.FilePath));
            pbar.WriteLine("Parsing done. Writing streams.");
            foreach (var kvp in csvStreams)
            {
                await kvp.Value.FileStream.FlushAsync();
                kvp.Value.FileStream.Close();
                await kvp.Value.FileStream.DisposeAsync();
            }
            pbar.Dispose();
            Console.WriteLine($"Found {objectCount:n0} {typeName}s. Wrote them to {csvFileNames}.");
        }
    }
}