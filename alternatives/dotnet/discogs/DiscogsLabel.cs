using System.Collections.Generic;
using System.Xml.Serialization;

namespace discogs.Labels
{
    public class label : IExportToCsv
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
        public string contactinfo { get; set; }
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
        public IReadOnlyDictionary<string, string[]> GetCsvExportScheme() => CsvExportHeaders;

        /// <summary>
        /// Exports instance to CSV.
        /// </summary>
        /// <returns>Tuples where the StreamName matches a key from <see ref="GetCsvExportScheme"> </returns>
        public IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv()
        {
            yield return ("label", new[] { this.id, this.name, this.contactinfo, this.profile, this.parentLabel?.name, this.data_quality });
            if (urls?.Length > 0)
            {
                foreach (var url in urls)
                {
                    if (string.IsNullOrEmpty(url)) continue;
                    yield return ("label_url", new[] { this.id, url });
                }
            }
            if (images?.Length > 0)
            {
                foreach (var image in this.images)
                {
                    yield return ("label_image", new[] { this.id, image.type, image.width, image.height });
                }
            }
        }
    }

    public class parentLabel
    {
        [XmlAttribute]
        public string id { get; set; }

        [XmlText]
        public string name { get; set; }
    }
}