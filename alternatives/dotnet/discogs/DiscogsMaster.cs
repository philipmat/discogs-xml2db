
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
        public string main_release { get; set; }
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
            yield return ("master", new[] { id, title, year, main_release, data_quality });
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

        public bool IsValid() => !string.IsNullOrEmpty(id);

        public void Populate(XmlReader reader)
        {
            if (reader.Name != "master")
            {
                return;
            }

            // <master id="123"> unlike all others
            this.id = reader.GetAttribute("id");
            while (reader.Read())
            {
                if (reader.IsStartElement("master"))
                {
                    // that means we encountered the next node
                    return;
                }
                if (reader.IsStartElement("main_release"))
                {
                    this.main_release = reader.ReadElementContentAsString();
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
                if (reader.IsStartElement("artists"))
                {
                    var list = new List<artist>();
                    reader.Read();
                    while (reader.IsStartElement("artist"))
                    {
                        var artist = new artist();
                        while (reader.Read() &&
                            (
                            reader.IsStartElement("id") ||
                            reader.IsStartElement("name") ||
                            reader.IsStartElement("anv") ||
                            reader.IsStartElement("join") ||
                            reader.IsStartElement("role") ||
                            reader.IsStartElement("tracks")))
                        {
                            /*
                            var tagName = reader.Name;
                            var value = reader.ReadElementContentAsString();
                            switch (tagName)
                            {
                                case "id":
                                    artist.id = value;
                                    break;
                                case "name":
                                    artist.name = value;
                                    break;
                                case "anv":
                                    artist.anv = value;
                                    break;
                                case "join":
                                    artist.join = value;
                                    break;
                                case "role":
                                    artist.role = value;
                                    break;
                                case "tracks":
                                    artist.tracks = value;
                                    break;
                                default:
                                    break;
                            }
                            */
                            {
                                if (reader.IsStartElement("id"))
                                    artist.id = reader.ReadElementContentAsString();
                                if (reader.IsStartElement("name"))
                                    artist.name = reader.ReadElementContentAsString();
                                if (reader.IsStartElement("anv"))
                                    artist.anv = reader.ReadElementContentAsString();
                                if (reader.IsStartElement("join"))
                                    artist.join = reader.ReadElementContentAsString();
                                if (reader.IsStartElement("role"))
                                    artist.role = reader.ReadElementContentAsString();
                                if (reader.IsStartElement("tracks"))
                                    artist.tracks = reader.ReadElementContentAsString();
                            }
                        }
                        list.Add(artist);
                        if (!reader.IsStartElement("artist"))
                        {
                            reader.ReadEndElement();
                        }
                    }
                    this.artists = list.ToArray();
                }
                if (reader.IsStartElement("genres"))
                {
                    reader.Read();
                    var list = new List<string>();
                    while (reader.IsStartElement("genre"))
                    {
                        var e = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(e))
                            list.Add(e);
                    }
                    this.genres = list.ToArray();
                }
                if (reader.IsStartElement("styles"))
                {
                    reader.Read();
                    var list = new List<string>();
                    while (reader.IsStartElement("style"))
                    {
                        var e = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(e))
                            list.Add(e);
                    }
                    this.styles = list.ToArray();
                }
                if (reader.IsStartElement("year"))
                {
                    this.year = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("title"))
                {
                    this.title = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("data_quality"))
                {
                    this.data_quality = reader.ReadElementContentAsString();
                }
                if (reader.IsStartElement("videos"))
                {
                    var list = new List<video>();
                    reader.Read();
                    while (reader.IsStartElement("video"))
                    {
                        var video = new video
                        {
                            src = reader.GetAttribute("src"),
                            duration = reader.GetAttribute("duration"),
                            embed = reader.GetAttribute("embed"),
                        };
                        while (reader.Read()
                            && (reader.IsStartElement("title") || reader.IsStartElement("description"))) {
                            if (reader.IsStartElement("title"))
                            {
                                video.title = reader.ReadElementContentAsString();
                            }
                            if (reader.IsStartElement("description"))
                            {
                                video.description = reader.ReadElementContentAsString();
                            }
                        }
                        list.Add(video);
                        if (!reader.IsStartElement("video"))
                        {
                            reader.ReadEndElement();
                        }
                    }
                    this.videos = list.ToArray();
                }

            }
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