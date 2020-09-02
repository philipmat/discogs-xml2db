using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace discogs.Labels
{
    public class label : IExportable
    {
        private static readonly Dictionary<string, string[]> CsvExportHeaders = new Dictionary<string, string[]>
        {
            ["label"] = new[] { "id", "name", "contact_info", "profile", "parent_name", "data_quality" },
            ["label_url"] = new[] { "label_id", "url" },
            ["label_image"] = new[] { "label_id", "type", "width", "height" },
        };

        public image[] images { get; set; }
        public string id { get; set; }
        public string name { get; set; }

        private string contactinfo;


        public string profile { get; set; }
        public string data_quality { get; set; }
        public parentLabel parentLabel { get; set; }

        [XmlArrayItem("url")]
        public string[] urls { get; set; }

        [XmlAttribute("id")]
        public string SubId { get; set; }

        [XmlText]
        public string SubName { get; set; }

        public label[] sublabels { get; set; }

        /// <summary>
        /// Gets the possible export schemes for the class
        /// </summary>
        /// <returns>A read-only dictionary where the key is the type of export stream and the values are the headers/columns/fields exported.</returns>
        public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => CsvExportHeaders;

        /// <summary>
        /// Exports instance to CSV.
        /// </summary>
        /// <returns>Tuples where the StreamName matches a key from <see ref="GetCsvExportScheme"> </returns>
        public IEnumerable<(string StreamName, string[] RowValues)> Export()
        {
            yield return ("label", new[] { this.id, this.name, this.contactinfo, this.profile, this.parentLabel?.name, this.data_quality });
            if ((urls?.Length ?? 0) > 0)
            {
                foreach (var url in urls)
                {
                    if (string.IsNullOrEmpty(url)) continue;
                    yield return ("label_url", new[] { this.id, url });
                }
            }
            if ((images?.Length ?? 0) > 0)
            {
                foreach (var image in this.images)
                {
                    yield return ("label_image", new[] { this.id, image.type, image.width, image.height });
                }
            }
        }

        public void Populate(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.IsStartElement("label"))
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
                if (reader.IsStartElement("contactinfo"))
                {
                    this.contactinfo = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("profile"))
                {
                    this.profile = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("data_quality"))
                {
                    this.data_quality = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("parentLabel"))
                {
                    this.parentLabel = new discogs.Labels.parentLabel
                    {
                        id = reader.GetAttribute("id"),
                        name = reader.ReadElementContentAsString()
                    };
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

        public bool IsValid() => !string.IsNullOrEmpty(id);
    }


    public class parentLabel
    {
        [XmlAttribute]
        public string id { get; set; }

        [XmlText]
        public string name { get; set; }
    }
}