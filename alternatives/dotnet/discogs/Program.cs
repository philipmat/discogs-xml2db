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
        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args.Select((s, i) => $"{i,-2} - {s}")));
            var fileName = args[^1];
            if (Path.GetFileName(fileName).Contains("discogs")) {
                fileName = Path.GetFullPath(fileName);
                Console.WriteLine($"Variant2: {fileName}");
                if (fileName.Contains("_labels")) {
                    await Variant2<discogs.Labels.label>(fileName);
                } else if (fileName.Contains("_releases")) {
                    await Variant2<discogs.Releases.release>(fileName);
                }
            }
            else if (Path.GetFileName(fileName) == "label.xml") {
                DeserializeLabelToJson(fileName);
            }
            else if (fileName == "serialize-label") {
                SerializeLabel();
            }
            else if (Path.GetFileName(fileName) == "release.xml") {
                DeserializeReleaseToJson(fileName);
            }
        }

        private static void DeserializeLabelToJson(string fileName)
        {
            using var reader = new StreamReader(fileName);

            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(discogs.Labels.label));
            var label = (discogs.Labels.label) _labelXmlSerializer.Deserialize(reader);
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
            };
            var labelJson = JsonSerializer.Serialize(label, jsonOptions);
            Console.WriteLine($@"JSON label:
 {labelJson}");
        }

        private static void DeserializeReleaseToJson(string fileName)
        {
            using var reader = new StreamReader(fileName);
            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(discogs.Releases.release));
            var obj = (discogs.Releases.release) _labelXmlSerializer.Deserialize(reader);
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
            };
            var labelJson = JsonSerializer.Serialize(obj, jsonOptions);
            Console.WriteLine($@"JSON {obj.GetType().Name}:
 {labelJson}");
        }

        private static T Deserialize<T>(string content)
        {
            using var reader = new StringReader(content);
            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(T));
            var label = (T) _labelXmlSerializer.Deserialize(reader);
            return label;
        }

        private static void SerializeLabel() {
            var label = new discogs.Labels.label {
                images = new [] {
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
            var xmlSettings = new XmlWriterSettings {
                Indent = true,
            };
            using var xmlWriter = XmlWriter.Create(writer);
            xml.Serialize(writer, label);
            var labelXml = writer.ToString();
            Console.WriteLine($@"XML label:
{labelXml}");
        }

        private static Dictionary<string, (string FilePath, StreamWriter FileStream)> GetCsvFilesFor<T>(string xmlFile)
            where T: IExportToCsv, new()
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
            foreach(var (streamName, row) in csvExports) {
                await streams[streamName].FileStream.WriteLineAsync(CsvExtensions.ToCsv(row));
            }
        }

        static async Task Variant2<T>(string fileName)
            where T: IExportToCsv, new()
        {
            var typeName = typeof(T).Name.Split('.')[^1];
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvStreams = GetCsvFilesFor<T>(fileName);
            int objectCount = 0;
            using FileStream fileStream = new FileStream(fileName, FileMode.Open);
            Stream readingStream = fileStream;
            if (System.IO.Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase)) {
                readingStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            var settings = new XmlReaderSettings {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
            };
            using XmlReader reader = XmlReader.Create(readingStream, settings);

            while (reader.Read())
            {
                if (reader.Name == typeName)
                {
                    var objectString = await reader.ReadOuterXmlAsync();
                    await WriteCsvAsync<T>(objectString, csvStreams);
                    // var labelId = DeserializeLabel(labelString);
                    // Console.WriteLine($"label: {labelId}");
                    // Console.Write('.');
                    // _ = reader.ReadOuterXml();
                    objectCount++;
                    // await reader.SkipAsync();
                    continue;
                }
            }
            var csvFileNames = string.Join("; ", csvStreams.Select(kvp => kvp.Value.FilePath));
            foreach(var kvp in csvStreams) {
                await kvp.Value.FileStream.FlushAsync();
                kvp.Value.FileStream.Close();
                await kvp.Value.FileStream.DisposeAsync();
            }
            Console.WriteLine($"Found {objectCount:n0} {typeName}s. Wrote them to {csvFileNames}.");
        }
    }
}