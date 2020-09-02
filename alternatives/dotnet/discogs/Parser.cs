using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace discogs
{
    public class Parser<T>
            where T : IExportable, new()
    {
        private const int BufferSize = 1024 * 1024;
        private static readonly XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(T));
        private readonly int _throttle = 1;
        private readonly string _typeName;
        private readonly IExporter<T> _exporter;
        public Parser(IExporter<T> exporter, int throttle = 1)
        {
            _exporter = exporter;
            _throttle = throttle;
            _typeName = typeof(T).Name.Split('.')[^1];
        }

        public event EventHandler<ParseEventArgs> OnSucessfulParse = delegate { };

        public async Task ParseFileAsync(string fileName)
        {
            using FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BufferSize, useAsync: true);
            Stream readingStream = fileStream;
            if (System.IO.Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase))
            {
                readingStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            await ParseStreamAsync(readingStream);
            await readingStream.DisposeAsync();
        }

        public Task ParseStreamAsync(Stream stream) => ParseStreamAsync2(stream);

        public async Task ParseStreamAsync1(Stream stream)
        {
            int objectCount = 0;
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                // TODO: perf IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                XmlResolver = null,
            };
            using XmlReader reader = XmlReader.Create(stream, settings);

            await reader.MoveToContentAsync();
            await reader.ReadAsync();
            while (!reader.EOF)
            {
                if (reader.Name == _typeName)
                {
                    var objectString = await reader.ReadOuterXmlAsync();
                    // var objectString = await reader.ReadInnerXmlAsync();
                    try
                    {
                        if (!string.IsNullOrEmpty(objectString))
                        {
                            await ExportRecord(objectString);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error {ex} parsing node {objectString}");
                    }
                    objectCount++;
                    // await reader.SkipAsync();
                    if (objectCount % _throttle == 0) OnSucessfulParse(null, new ParseEventArgs { ParseCount = objectCount });
                    continue;
                }
                else
                {
                    await reader.ReadAsync();
                }
            }
            await _exporter.CompleteExportAsync(objectCount);
        }
        public async Task ParseStreamAsync2(Stream stream)
        {
            int objectCount = 0;
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
            };
            using XmlReader reader = XmlReader.Create(stream, settings);

            await reader.MoveToContentAsync();
            await reader.ReadAsync();
            while (!reader.EOF)
            {
                if (reader.Name == _typeName)
                {
                    var lbl = new discogs.Labels.label();
                    while (reader.Read())
                    {
                        if (reader.IsStartElement(_typeName))
                            break;
                        if (reader.IsStartElement("id"))
                            lbl.id = reader.ReadElementContentAsString();
                        if (reader.IsStartElement("name"))
                            lbl.name = reader.ReadElementContentAsString();
                        if (reader.IsStartElement("contactinfo"))
                            lbl.contactinfo = reader.ReadElementContentAsString();
                        if (reader.IsStartElement("profile"))
                            lbl.profile = reader.ReadElementContentAsString();
                        if (reader.IsStartElement("data_quality"))
                            lbl.data_quality = reader.ReadElementContentAsString();
                        if (reader.IsStartElement("parentLabel"))
                            lbl.parentLabel = new discogs.Labels.parentLabel { id = reader.GetAttribute("id"), name = reader.ReadElementContentAsString() };
                        if (reader.IsStartElement("images"))
                        {
                            var images = new List<image>();
                            while (reader.Read() && reader.IsStartElement("image"))
                            {
                                var image = new image { type = reader.GetAttribute("type"), width = reader.GetAttribute("width"), height = reader.GetAttribute("height") };
                                images.Add(image);
                            }
                            lbl.images = images.ToArray();
                        }
                        if (reader.IsStartElement("urls"))
                        {
                            reader.Read();
                            var urls = new List<string>();
                            while (reader.IsStartElement("url"))
                            {
                                var url = reader.ReadElementContentAsString();
                                if (!string.IsNullOrWhiteSpace(url))
                                    urls.Add(url);
                            }
                            lbl.urls = urls.ToArray();
                        }
                    }
                    if (lbl.id is null)
                        continue;
                    var obj = (T)(object)lbl;
                    await _exporter.ExportAsync(obj);

                    objectCount++;
                    if (objectCount % _throttle == 0) OnSucessfulParse(null, new ParseEventArgs { ParseCount = objectCount });
                }
                else
                {
                    await reader.ReadAsync();
                }
            }
            await _exporter.CompleteExportAsync(objectCount);
        }

        protected static T Deserialize(string content)
        {
            // TODO: would MemoryStream be faster?
            using var reader = new StringReader(content);
            var obj = (T)_labelXmlSerializer.Deserialize(reader);
            return obj;
        }

        private async Task ExportRecord(string objectString)
        {
            var obj = Deserialize(objectString);
            await _exporter.ExportAsync(obj);
        }
    }

    public class ParseEventArgs : EventArgs
    {
        public int ParseCount { get; set; }
    }
}