using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace discogs
{
    public class SerializerParser<T> : Parser<T>
            where T : IExportable, new()
    {
        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(T));

        public SerializerParser(IExporter<T> exporter, int throttle = 1)
            :base(exporter, throttle)
        {
        }

        protected override async Task<T> ReadObject(XmlReader positionedReader)
        {
            var objectString = await positionedReader.ReadOuterXmlAsync();
            if (string.IsNullOrEmpty(objectString))
            {
                return default(T);
            }
            try
            {
                return Deserialize(objectString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex} parsing node {objectString}");
                return default(T);
            }
        }

        protected T Deserialize(string objectString)
        {
            using var reader = new StringReader(objectString);
            var obj = (T)_serializer.Deserialize(reader);
            return obj;
        }
    }
}