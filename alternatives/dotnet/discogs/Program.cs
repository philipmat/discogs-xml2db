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
        private static readonly XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(discogs.label));

        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args.Select((s, i) => $"{i,-2} - {s}")));
            var fileName = args[^1];
            if (Path.GetFileName(fileName).Contains("discogs")) {
                fileName = Path.GetFullPath(fileName);
                Console.WriteLine($"Variant2: {fileName}");
                await Variant2(fileName);
            }
            else if (Path.GetFileName(fileName) == "label.xml") {
                DeserializeLabelToJson(fileName);
            }
            else if (fileName == "serialize-label") {
                SerializeLabel();
            }
        }

        private static void DeserializeLabelToJson(string fileName)
        {
            using var reader = new StreamReader(fileName);
            var label = (discogs.label) _labelXmlSerializer.Deserialize(reader);
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
            };
            var labelJson = JsonSerializer.Serialize(label, jsonOptions);
            Console.WriteLine($@"JSON label:
 {labelJson}");
        }

        private static discogs.label DeserializeLabel(string content){
            using var reader = new StringReader(content);
            var label = (discogs.label) _labelXmlSerializer.Deserialize(reader);
            return label;
        }

        private static void SerializeLabel() {
            var label = new discogs.label {
                images = new [] {
                    new discogs.image { type = "primary", uri="", uri150="", width="132", height="24"}
                },
                contactinfo = @"Planet
                E
                Communication",
                parentLabel = new parentLabel { id = "123", name = "The Parent Label" },
                data_quality = "Correct",
                id = "1",
                name = "Planet E",
                urls = new[] {
                    // new discogs.url { TheUrl = "http://planet-e.net" }
                     "http://planet-e.net"
                },
                sublabels = new[] {
                    new label { SubId = "2", SubName = "Antidote (4)"}
                }
            };
            var xml = new XmlSerializer(typeof(discogs.label));

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

        private static Dictionary<string, (string FilePath, StreamWriter FileStream)> GetCsvFilesForLabel(string xmlFile) {
            var dir = Path.GetDirectoryName(xmlFile);
            IReadOnlyDictionary<string, string[]> files = discogs.label.GetCsvExportScheme();
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvFiles = files.ToDictionary(
                kvp => kvp.Key, 
                kvp => {
                    var csvFile = Path.Combine(dir, $"{kvp.Key}.csv");
                    var stream = new StreamWriter(csvFile);
                    stream.WriteLine(CsvExtensions.ToCsv(kvp.Value));
                    return (csvFile, stream);
            });

            return csvFiles;
        }

        private static async Task WriteCsvAsync(string labelString, Dictionary<string, (string FilePath, StreamWriter FileStream)> streams) {
            discogs.label labelObj = DeserializeLabel(labelString);
            IEnumerable<(string StreamName, string[] Row)> csvExports = labelObj.ExportToCsv();
            foreach(var (streamName, row) in csvExports) {
                await streams[streamName].FileStream.WriteLineAsync(CsvExtensions.ToCsv(row));
            }
        }

        static async Task Variant2(string fileName){
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvStreams = GetCsvFilesForLabel(fileName);
            int labelCount = 0;
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
                if (reader.Name == "label")
                {
                    var labelString = await reader.ReadOuterXmlAsync();
                    await WriteCsvAsync(labelString, csvStreams);
                    // var labelId = DeserializeLabel(labelString);
                    // Console.WriteLine($"label: {labelId}");
                    // Console.Write('.');
                    // _ = reader.ReadOuterXml();
                    labelCount++;
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
            Console.WriteLine($"Found {labelCount:n0} label. Wrote them to {csvFileNames}.");
        }

        static XStreamingElement Variant1(string fileName)
            => new XStreamingElement("Root",
                from el in StreamCustomerItem(fileName)
                select new XElement("Item",
                    new XElement("Customer", (string) el.Parent.Element("Name")),
                    new XElement(el.Element("Key"))
                )
            );
        static XElement Variant0(string fileName)
            => new XElement("Root",  
                    from el in StreamCustomerItem(fileName)  
                    where (int)el.Element("Key") >= 3 && (int)el.Element("Key") <= 7  
                    select new XElement("Item",  
                        new XElement("Customer", (string)el.Parent.Element("Name")),  
                        new XElement(el.Element("Key"))  
                    )  
                );  

        static IEnumerable<XElement> StreamCustomerItem(string uri)
        {
            using (XmlReader reader = XmlReader.Create(uri))
            {
                XElement name = null;
                XElement item = null;

                reader.MoveToContent();

                // Parse the file, save header information when encountered, and yield the  
                // Item XElement objects as they are created.  

                // loop through Customer elements  
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.Name == "label")
                    {
                        // move to Name element  
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element &&
                                reader.Name == "id")
                            {
                                name = XElement.ReadFrom(reader) as XElement;
                                break;
                            }
                        }

                        // loop through Item elements  
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement)
                                break;
                            if (reader.NodeType == XmlNodeType.Element
                                && reader.Name == "id")
                            {
                                item = XElement.ReadFrom(reader) as XElement;
                                if (item != null)
                                {
                                    XElement tempRoot = new XElement("Root",
                                        new XElement(name)
                                    );
                                    tempRoot.Add(item);
                                    yield return item;
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}