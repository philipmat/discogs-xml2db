using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Text.Json;

namespace discogs
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args.Select((s, i) => $"{i,-2} - {s}")));
            var fileName = args[^1];
            if (Path.GetFileName(fileName).Contains("discogs")) {
                Console.WriteLine($"Variant2: {fileName}");
                await Variant2(fileName);
            }
            else if (Path.GetFileName(fileName) == "label.xml") {
                DeserializeLabel(fileName);
            }
            else if (fileName == "serialize-label") {
                SerializeLabel();
            }
        }

        private static void DeserializeLabel(string fileName)
        {
            var xml = new XmlSerializer(typeof(discogs.label));
            using var reader = new StreamReader(fileName);
            var label = (discogs.label) xml.Deserialize(reader);
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
            };
            var labelJson = JsonSerializer.Serialize(label, jsonOptions);
            Console.WriteLine($@"JSON label:
 {labelJson}");
        }

        private static void SerializeLabel() {
            var label = new discogs.label {
                images = new [] {
                    new discogs.image { type = "primary", uri="", uri150="", width=132, height=24}
                },
                contactinfo = @"Planet
                E
                Communication",
                data_quality = "Correct",
                id = 1,
                name = "Planet E",
                urls = new[] {
                    // new discogs.url { TheUrl = "http://planet-e.net" }
                     "http://planet-e.net"
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

        static async Task Variant2(string fileName){
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
                    _ = await reader.ReadOuterXmlAsync();
                    // _ = reader.ReadOuterXml();
                    labelCount++;
                    // await reader.SkipAsync();
                    continue;
                }
            }
            Console.WriteLine($"Found {labelCount:n0} label.");
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