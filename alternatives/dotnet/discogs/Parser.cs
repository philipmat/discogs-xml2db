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
            where T : IExportToCsv, new()
    {
        private const int BufferSize = 1024 * 1024;
        private readonly int _throttle = 1;
        private readonly string _typeName;
        private readonly IExporter<T> _exporter;
        public Parser(IExporter<T> exporter, int throttle = 1)
        {
            _exporter = exporter;
            _throttle = throttle;
            _typeName = typeof(T).Name.Split('.')[^1];
        }

        public event EventHandler<ParseEventArgs> OnSucessfulParse = delegate {};

        public async Task ParseFileAsync(string fileName) {
            using FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BufferSize, useAsync: true);
            Stream readingStream = fileStream;
            if (System.IO.Path.GetExtension(fileName).Equals(".gz", StringComparison.OrdinalIgnoreCase))
            {
                readingStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            await ParseStreamAsync(readingStream);
            await readingStream.DisposeAsync();
        }

        public async Task ParseStreamAsync(Stream stream)
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

        protected static T Deserialize(string content)
        {
            using var reader = new StringReader(content);
            XmlSerializer _labelXmlSerializer = new XmlSerializer(typeof(T));
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