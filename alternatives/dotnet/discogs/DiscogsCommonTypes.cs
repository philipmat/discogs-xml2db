using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace discogs
{
    public interface IExportable
    {
        IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields();
        IEnumerable<(string StreamName, string[] RowValues)> Export();
        public bool IsValid();
        void Populate(XmlReader reader);
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

        internal static image[] ParseImages(XmlReader reader)
        {
            var images = new List<image>();
            while (reader.Read() && reader.IsStartElement("image"))
            {
                var image = ParseImage(reader);
                images.Add(image);
            }
            return images.ToArray();
        }

        internal static image ParseImage(XmlReader reader)
            => new image {
                type = reader.GetAttribute("type"),
                width = reader.GetAttribute("width"),
                height = reader.GetAttribute("height")
            };
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