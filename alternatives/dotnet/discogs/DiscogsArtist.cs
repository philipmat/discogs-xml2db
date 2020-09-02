using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace discogs.Artists
{
    public class artist : IExportable
    {
        private static readonly Dictionary<string, string[]> CsvExportHeaders = new Dictionary<string, string[]>
        {
            { "artist", "id name realname profile data_quality".Split(" ") },
            { "artist_alias", "artist_id alias_name".Split(" ") },
            { "artist_namevariation", "artist_id name".Split(" ") },
            { "artist_url", "artist_id url".Split(" ") },
            { "group_member", "group_artist_id member_artist_id member_name".Split(" ") },
            { "artist_image", "artist_id type width height".Split(" ") },
        };

        public image[] images { get; set; }
        [XmlArrayItem("url")]
        public string[] urls { get; set; }

        public string id { get; set; }
        public string name { get; set; }
        public string realname { get; set; }
        public string profile { get; set; }
        public string data_quality { get; set; }
        [XmlArrayItem("name")]
        public string[] namevariations { get; set; }
        public name[] members { get; set; }
        public name[] aliases { get; set; }
        // groups is not parsed in the python version
        // public name[] groups {get;set;}

        public IEnumerable<(string StreamName, string[] RowValues)> Export()
        {
            yield return ("artist", new[] { id, this.name, realname, profile, data_quality });
            foreach (var a in (aliases ?? System.Array.Empty<name>()))
            {
                yield return ("artist_alias", new[] { id, a.value });
            }
            foreach (var nv in (namevariations ?? System.Array.Empty<string>()))
            {
                yield return ("artist_namevariation", new[] { id, nv });
            }
            foreach (var u in (urls ?? System.Array.Empty<string>()))
            {
                yield return ("artist_url", new[] { id, u });
            }
            foreach (var m in (members ?? System.Array.Empty<name>()))
            {
                yield return ("group_member", new[] { id, m.id, m.value });
            }
            if ((images?.Length ?? 0) > 0)
            {
                foreach (var image in this.images)
                {
                    yield return ("artist_image", new[] { this.id, image.type, image.width, image.height });
                }
            }
        }

        public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
            => CsvExportHeaders;

        public bool IsValid() => !string.IsNullOrEmpty(id);

        public void Populate(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.IsStartElement("artist"))
                {
                    return;
                }
                if (reader.IsStartElement("id"))
                {
                    this.id = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("name"))
                {
                    this.name = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("realname"))
                {
                    this.realname = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("profile"))
                {
                    this.profile = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("data_quality"))
                {
                    this.data_quality = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("namevariations"))
                {
                    reader.Read();
                    var nvs = new List<string>();
                    while (reader.IsStartElement("name"))
                    {
                        var nv = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(nv))
                            nvs.Add(nv);
                    }
                    this.namevariations = nvs.ToArray();
                }
                /* FIXME:
                if (reader.IsStartElement("members"))
                {
                    reader.Read(); // move inside members
                    var members = new List<name>();
                    while (reader.IsStartElement("name") || reader.IsStartElement("id"))
                    {
                        if (reader.IsStartElement("id"))
                        {
                            reader.Read();
                            continue;
                        }
                        var n = new name
                        {
                            id = reader.GetAttribute("id"),
                            value = reader.ReadElementContentAsString(),
                        };
                        members.Add(n);
                    }
                    this.members = members.ToArray();
                }
                */
                if (reader.IsStartElement("aliases"))
                {
                    reader.Read();
                    var aliases = new List<name>();
                    while (reader.IsStartElement("name"))
                    {
                        var n = new name
                        {
                            id = reader.GetAttribute("id"),
                            value = reader.ReadElementContentAsString(),
                        };
                        aliases.Add(n);
                    }
                    this.aliases = aliases.ToArray();
                }

                if (reader.IsStartElement("images"))
                {
                    var images = new List<image>();
                    while (reader.Read() && reader.IsStartElement("image"))
                    {
                        var image = new image { type = reader.GetAttribute("type"), width = reader.GetAttribute("width"), height = reader.GetAttribute("height") };
                        images.Add(image);
                    }
                    this.images = images.ToArray();
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
                    this.urls = urls.ToArray();
                }
            }
        }
    }

    public class name
    {
        [XmlAttribute]
        public string id { get; set; }
        [XmlText]
        public string value { get; set; }
    }
}