using System.Collections.Generic;
using System.Xml.Serialization;

namespace discogs.Artists
{
    public class artist : IExportToCsv
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
        public string name {get;set;}
        public string realname { get; set; }
        public string profile { get; set; }
        public string data_quality { get; set; }
        [XmlArrayItem("name")]
        public string[] namevariations { get; set; }
        public name[] members {get;set;}
        public name[] aliases {get;set;}
        // groups is not parsed in the python version
        // public name[] groups {get;set;}

        public IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv()
        {
            yield return ("artist", new[] { id, this.name, realname, profile, data_quality });
            foreach(var a in (aliases ?? System.Array.Empty<name>())) {
                yield return ("artist_alias", new[] { id, a.value });
            }
            foreach(var nv in (namevariations ?? System.Array.Empty<string>())) {
                yield return ("artist_alias", new[] { id, nv });
            }
            foreach(var u in (urls ?? System.Array.Empty<string>())) {
                yield return ("artist_url", new[] { id, u });
            }
            foreach(var m in (members ?? System.Array.Empty<name>())) {
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

        public IReadOnlyDictionary<string, string[]> GetCsvExportScheme()
            => CsvExportHeaders;
    }

    public class name
    {
        [XmlAttribute]
        public string id { get; set; }
        [XmlText]
        public string value { get; set; }
    }
}