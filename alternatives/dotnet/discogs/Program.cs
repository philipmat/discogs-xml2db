using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.IO.Compression;

namespace discogs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args.Select((s, i) => $"{i,-2} - {s}")));
            Console.WriteLine($"Variant2: {args[^1]}");
            await Variant2(args[^1]);
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

        public class DiscogsLabel {
            public DiscogsImage Images {get;set;}
            public int Id {get;set;}
            public string Name { get; set; }
            public string ContactInfo { get; set; }
            public string DataQuality {get;set;}
            public DiscogsUrl[] Urls { get; set; }

        }
        public class DiscogsImage {
            public string Type { get; set; }
            public string Uri { get; set; }
            public string Uri150 { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
        public class DiscogsUrl {
            public string Url { get; set; }
        }
        public class DiscogsSubLabel {
            public int Id { get; set; }
            public string Text {get;set;}
        }
    }
}