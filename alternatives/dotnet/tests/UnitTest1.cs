using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using discogs;
using Xunit;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

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
    }
}
