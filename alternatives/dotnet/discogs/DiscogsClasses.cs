using System.Xml.Serialization;

namespace discogs
{
    public class label
    {
        public image[] images { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string contactinfo { get; set; }
        public string data_quality { get; set; }
        [XmlArrayItem("url")]
        public string[] urls { get; set; }
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
        public int width { get; set; }
        [XmlAttribute]
        public int height { get; set; }
    }

    public class url
    {
        [XmlElement("url")]
        public string TheUrl { get; set; }
    }

    [XmlRoot("label")]
    public class sublabel
    {
        [XmlAttribute]
        public int id { get; set; }
        public string label { get; set; }
    }
}