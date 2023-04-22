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

        internal static image[] Parse(XmlReader reader)
        {
            var list = new List<image>();
            while (reader.Read() && reader.IsStartElement("image"))
            {
                var obj = ParseImage(reader);
                list.Add(obj);
            }
            return list.ToArray();
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

        internal static video[] Parse(XmlReader reader)
        {
            var list = new List<video>();
            while (reader.Read() && reader.IsStartElement("video"))
            {
                var one = new video {
                    src = reader.GetAttribute("src"),
                    duration = reader.GetAttribute("duration"),
                    embed = reader.GetAttribute("embed"),
                };

                reader.Read();
                while(!reader.EOF)
                {
                     if (reader.Name == "title") {
                        one.title = reader.ReadElementContentAsString();
                        continue;
                     }
                     if (reader.Name == "description")
                     {
                        one.description = reader.ReadElementContentAsString();
                        continue;
                     }
                     if (reader.Name == "video") {
                         // reader.Skip();
                         break;
                     }

                     // any other element
                     reader.Read();
                }

                list.Add(one);
            }
            return list.ToArray();
        }
    }
}