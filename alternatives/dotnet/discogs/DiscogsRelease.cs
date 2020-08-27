using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace discogs.Releases
{
    public class release : IExportToCsv
    {
        private static readonly Dictionary<string, string[]> CsvExportHeaders = new Dictionary<string, string[]>
        {
            { "release", "id title released country notes data_quality master_id status".Split(" ") },
            { "release_genre", "release_id genre".Split(" ") },
            { "release_label", "release_id label_name catno".Split(" ") },
            { "release_style", "release_id style".Split(" ") },
            { "release_image", "release_id type width height".Split(" ") },
            { "release_format", "release_id name qty text_string descriptions".Split(" ") },
            { "release_identifier", "release_id description type value".Split(" ") },
            { "release_company", "release_id company_id company_name entity_type entity_type_name uri".Split(" ") },
            { "release_video", "release_id duration title description uri".Split(" ") },
            { "release_artist", "release_id artist_id artist_name extra anv position join_string role tracks".Split(" ") },
            { "release_track", "release_id sequence position parent title duration track_id".Split(" ") },
            { "release_track_artist", "release_id track_sequence track_id artist_id artist_name extra anv position join_string role tracks".Split(" ") },
        };

        [XmlAttribute]
        public string id { get; set; }

        [XmlAttribute]
        public string status { get; set; }
        public image[] images { get; set; }
        public artist[] artists { get; set; }
        public string title { get; set; }
        public label[] labels { get; set; }
        public artist[] extraartists { get; set; }
        public format[] formats { get; set; }
        [XmlArrayItem("genre")]
        public string[] genres { get; set; }
        [XmlArrayItem("style")]
        public string[] styles { get; set; }
        public string country { get; set; }
        public string released { get; set; }
        public string notes { get; set; }
        public string data_quality { get; set; }
        // has is_main_release="true,false" attribute
        public string master_id { get; set; }
        public track[] tracklist { get; set; }
        public identifier[] identifiers { get; set; }
        public video[] videos { get; set; }
        public company[] companies { get; set; }

        public IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv()
        {
            yield return ("release", new[] { id, title, released, country, notes, data_quality, master_id, status });
            if (genres?.Length > 0)
            {
                foreach (var g in genres)
                {
                    if (string.IsNullOrEmpty(g)) continue;
                    yield return ("release_genre", new[] { id, g });
                }
            }
            if (labels?.Length > 0)
            {
                foreach (var l in labels)
                {
                    if (l == null) continue;
                    yield return ("release_label", new[] { id, l.name, l.catno });
                }
            }
            if (styles?.Length > 0)
            {
                foreach (var s in styles)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    yield return ("release_style", new[] { id, s });
                }
            }
            if (images?.Length > 0)
            {
                foreach (var image in this.images)
                {
                    yield return ("release_image", new[] { this.id, image.type, image.width, image.height });
                }
            }
            if (formats?.Length > 0)
            {
                foreach (var f in formats)
                {
                    if (f == null) continue;
                    yield return ("release_format", new[] { id, f.name, f.qty, f.text, string.Join("; ", f.descriptions ?? System.Array.Empty<string>()) });
                }
            }
            if (identifiers?.Length > 0)
            {
                foreach (var i in identifiers)
                {
                    if (i == null) continue;
                    yield return ("release_identifier", new[] { id, i.description, i.type, i.value });
                }
            }
            if (companies?.Length > 0)
            {
                foreach (var c in companies)
                {
                    if (c == null) continue;
                    yield return ("release_company", new[] { id, c.id, c.name, c.entity_type, c.entity_type_name, c.resource_url });
                }
            }
            if (videos?.Length > 0)
            {
                foreach (var v in videos)
                {
                    if (v == null) continue;
                    yield return ("release_video", new[] { id, v.duration, v.title, v.description, v.src });
                }
            }
            if (artists?.Length > 0)
            {
                int position = 1;
                foreach (var a in artists)
                {
                    if (a == null) continue;
                    yield return ("release_artist", new[] { id, a.id, a.name, "0", a.anv, (position++).ToString(), a.join, a.role, a.tracks });
                }
            }
            if (extraartists?.Length > 0)
            {
                int position = 1;
                foreach (var a in extraartists)
                {
                    if (a == null) continue;
                    yield return ("release_artist", new[] { id, a.id, a.name, "1", a.anv, (position++).ToString(), a.join, a.role, a.tracks });
                }
            }
            if (tracklist?.Length > 0)
            {
                // TODO: this is more complex - sub-stracts, ect
                int seq = 1;
                foreach (var t in tracklist)
                {
                    if (t == null) continue;
                    yield return ("release_track", new[] { id, (seq++).ToString(), t.position, "TODO: parent track for subtrack", t.title, t.duration, "track_id" });
                }
            }
            /*
            if (tracklist?.Any(t => (t.artists?.Length ?? 0) > 0) == true)
            {
                foreach (var t in tracklist)
                {
                    if (t == null || (t?.artists?.Length ?? 0) == 0) continue;
                    int artistPosition = 1;
                    foreach (var a in t.artists)
                    {
                        if (a == null) continue;
                        yield return ("release_track_artist", new[] { id, t.position, "track_id", a.id, a.name, "0", a.anv, (artistPosition++).ToString(), a.join, a.role, a.tracks });
                    }
                    // TODO: extra artists
                }
            }
            */
        }

        public IReadOnlyDictionary<string, string[]> GetCsvExportScheme()
            => CsvExportHeaders;

    }

    public class artist
    {
        public string id { get; set; }
        public string name { get; set; }
        /// <summary>Artist name variation</summary>
        public string anv { get; set; }
        public string join { get; set; }
        public string role { get; set; }
        public string tracks { get; set; }
    }

    public class label
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public string catno { get; set; }
        [XmlAttribute]
        public string id { get; set; }
    }

    public class format
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public string qty { get; set; }
        [XmlAttribute]
        public string text { get; set; }
        [XmlArrayItem("description")]
        public string[] descriptions { get; set; }
    }

    public class track
    {
        public string position { get; set; }
        public string title { get; set; }
        public string duration { get; set; }
        public artist[] artists { get; set; }
    }
    public class identifier
    {
        [XmlAttribute]
        public string type { get; set; }
        [XmlAttribute]
        public string value { get; set; }
        [XmlAttribute]
        public string description { get; set; }
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
    public class company
    {
        public string id { get; set; }
        public string name { get; set; }
        public string catno { get; set; }
        public string entity_type { get; set; }
        public string entity_type_name { get; set; }
        public string resource_url { get; set; }
    }
}