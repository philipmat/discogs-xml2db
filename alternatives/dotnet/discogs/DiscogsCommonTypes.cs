using System.Collections.Generic;
using System.Xml.Serialization;

namespace discogs
{
    public interface IExportToCsv
    {
        IReadOnlyDictionary<string, string[]> GetCsvExportScheme();
        IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv();
    }

    public class image
    {
        [XmlAttribute]
        public string type { get; set; }
        [XmlAttribute]
        public string uri { get; set; }
        [XmlAttribute]
        public string uri150 { get; set; }
        [XmlAttribute]
        public string width { get; set; }
        [XmlAttribute]
        public string height { get; set; }
    }

    public class url
    {
        [XmlElement("url")]
        public string TheUrl { get; set; }
    }
    public class video
    {
        [XmlAttribute]
        public string src { get; set; }
        [XmlAttribute]
        public string duration { get; set; }
        [XmlAttribute]
        public string embed { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }
}