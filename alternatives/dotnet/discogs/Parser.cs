using System;
using System.IO;
using System.IO.Compression;
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
                DtdProcessing = DtdProcessing.Prohibit,
                // TODO: perf IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                XmlResolver = null,
            };
            using XmlReader reader = XmlReader.Create(stream, settings);

            await reader.MoveToContentAsync(); // moves to first element in XML
            await reader.ReadAsync(); // moves to the first element after, so the text between <artists> and <artist>
            while (!reader.EOF)
            {
                if (reader.Name == _typeName)
                {
                    if (reader.NodeType == XmlNodeType.EndElement) {
                        await reader.SkipAsync();
                        continue;
                    }

                    var obj = new T();
                    obj.Populate(reader);
                    if (!obj.IsValid())
                    {
                        continue;
                    }
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