
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace discogs.Masters
{
    public class master : IExportable
    {
        private static readonly Dictionary<string, string[]> CsvExportHeaders = new Dictionary<string, string[]>
        {
            { "master", "id title year main_release data_quality".Split(" ") },
            { "master_artist", "master_id artist_id artist_name anv position join_string role".Split(" ") },
            { "master_video", "master_id duration title description uri".Split(" ") },
            { "master_genre", "master_id genre".Split(" ") },
            { "master_style", "master_id style".Split(" ") },
            { "master_image", "master_id type width height".Split(" ") },
        };

        [XmlAttribute]
        public string id { get; set; }  
        public string main_release {get; set;}
        public string year { get; set; }
        public string title { get; set; }
        public string data_quality { get; set; }
        public image[] images { get; set; }
        public artist[] artists { get; set; }
        [XmlArrayItem("genre")]
        public string[] genres { get; set; }
        [XmlArrayItem("style")]
        public string[] styles { get; set; }
        public video[] videos { get; set; }

        public IEnumerable<(string StreamName, string[] RowValues)> Export()
        {
            yield return ("master", new [] { id, title, year, main_release, data_quality });
            if (artists?.Length > 0)
            {
                int position = 1;
                foreach (var a in artists)
                {
                    if (a == null) continue;
                    yield return ("master_artist", new[] { id, a.id, a.name, a.anv, (position++).ToString(), a.join, a.role /*, a.tracks*/ });
                }
            }
            if (videos?.Length > 0)
            {
                foreach (var v in videos)
                {
                    if (v == null) continue;
                    yield return ("master_video", new[] { id, v.duration, v.title, v.description, v.src });
                }
            }
            if (genres?.Length > 0)
            {
                foreach (var g in genres)
                {
                    if (string.IsNullOrEmpty(g)) continue;
                    yield return ("master_genre", new[] { id, g });
                }
            }
            if (styles?.Length > 0)
            {
                foreach (var s in styles)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    yield return ("master_style", new[] { id, s });
                }
            }
            if (images?.Length > 0)
            {
                foreach (var image in this.images)
                {
                    yield return ("master_image", new[] { this.id, image.type, image.width, image.height });
                }
            }
        }

        public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => CsvExportHeaders;

        public bool IsValid()
        {
            throw new System.NotImplementedException();
        }

        public void Populate(XmlReader reader)
        {
            throw new System.NotImplementedException();
        }
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
}