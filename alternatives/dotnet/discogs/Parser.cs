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

        private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            // TODO: perf IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            XmlResolver = null,
        };

        private readonly int _throttle = 1;
        private readonly string _typeName;
        private readonly IExporter<T> _exporter;
        public Parser(IExporter<T> exporter, int throttle = 1)
        {
            _exporter = exporter;
            _throttle = throttle;
            _typeName = typeof(T).Name.Split('.')[^1];
        }

        public static XmlReaderSettings DefaultReaderSettings => ReaderSettings;

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

        public async Task ParseStreamAsync(Stream stream)
        {
            int objectCount = 0;
            using XmlReader reader = XmlReader.Create(stream, ReaderSettings);

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
                    T obj = await ReadObject(reader);
                    if (obj?.IsValid() == false)
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

        protected virtual Task<T> ReadObject(XmlReader positionedReader)
        {
            var obj = new T();
            obj.Populate(positionedReader);
            return Task.FromResult(obj);
        }
    }

    public class ParseEventArgs : EventArgs
    {
        public int ParseCount { get; set; }
    }
}