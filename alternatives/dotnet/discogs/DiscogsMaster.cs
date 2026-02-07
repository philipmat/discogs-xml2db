namespace discogs.Masters;

public class master : IExportable
{
    private static readonly Dictionary<string, string[]> _csvExportHeaders = new()
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
        yield return ("master", [id, title, year, main_release, data_quality]);
        if (artists?.Length > 0)
        {
            int position = 1;
            foreach (var a in artists)
            {
                if (a == null) continue;
                yield return ("master_artist",
                    [id, a.id, a.name, a.anv, (position++).ToString(), a.join, a.role /*, a.tracks*/]);
            }
        }

        if (videos?.Length > 0)
        {
            foreach (var v in videos)
            {
                if (v == null) continue;
                yield return ("master_video", [id, v.duration, v.title, v.description, v.src]);
            }
        }

        if (genres?.Length > 0)
        {
            foreach (string g in genres)
            {
                if (string.IsNullOrEmpty(g)) continue;
                yield return ("master_genre", [id, g]);
            }
        }

        if (styles?.Length > 0)
        {
            foreach (string s in styles)
            {
                if (string.IsNullOrEmpty(s)) continue;
                yield return ("master_style", [id, s]);
            }
        }

        if (images?.Length > 0)
        {
            foreach (var image in images)
            {
                yield return ("master_image", [id, image.type, image.width, image.height]);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => _csvExportHeaders;

    public bool IsValid() => !string.IsNullOrEmpty(id);

    public void Populate(XmlReader reader) => Populate2(reader);


    public void Populate2(XmlReader reader)
    {
        if (reader.Name != "master")
        {
            return;
        }

        // <master id="123"> unlike all others
        id = reader.GetAttribute("id");
        reader.Read();
        while (!reader.EOF)
        {
            switch (reader.Name)
            {
                case "master":
                    // it's back on a master node (EndElement); release control
                    return;
                case "main_release":
                    main_release = reader.ReadElementContentAsString();
                    break;
                case "year":
                    year = reader.ReadElementContentAsString();
                    break;
                case "title":
                    title = reader.ReadElementContentAsString();
                    break;
                case "data_quality":
                    data_quality = reader.ReadElementContentAsString();
                    break;
                case "images":
                    images = image.Parse(reader);
                    break;
                case "genres":
                    genres = reader.ReadChildren("genre");
                    break;
                case "styles":
                    styles = reader.ReadChildren("style");
                    break;
                case "videos":
                    videos = video.Parse(reader);
                    break;
                case "artists":
                    artists = artist.Parse(reader);
                    break;
                default:
                    reader.Read();
                    break;
            }

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "master")
                {
                    return;
                }

                reader.Skip();
            }
        }
    }


    public void Populate1(XmlReader reader)
    {
        if (reader.Name != "master")
        {
            return;
        }

        // <master id="123"> unlike all others
        id = reader.GetAttribute("id");
        while (reader.Read())
        {
            if (reader.IsStartElement("master"))
            {
                // that means we encountered the next node
                return;
            }

            if (reader.IsStartElement("main_release"))
            {
                main_release = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("images"))
            {
                List<image> imageList = [];
                while (reader.Read() && reader.IsStartElement("image"))
                {
                    var image = new image
                    {
                        type = reader.GetAttribute("type"), width = reader.GetAttribute("width"),
                        height = reader.GetAttribute("height")
                    };
                    imageList.Add(image);
                }

                this.images = imageList.ToArray();
            }

            if (reader.IsStartElement("artists"))
            {
                List<artist> list = [];
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

                artists = list.ToArray();
            }

            if (reader.IsStartElement("genres"))
            {
                reader.Read();
                List<string> list = [];
                while (reader.IsStartElement("genre"))
                {
                    string e = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(e))
                        list.Add(e);
                }

                genres = list.ToArray();
            }

            if (reader.IsStartElement("styles"))
            {
                reader.Read();
                List<string> list = [];
                while (reader.IsStartElement("style"))
                {
                    string e = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(e))
                        list.Add(e);
                }

                styles = list.ToArray();
            }

            if (reader.IsStartElement("year"))
            {
                year = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("title"))
            {
                title = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("data_quality"))
            {
                data_quality = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("videos"))
            {
                List<video> list = [];
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
                           && (reader.IsStartElement("title") || reader.IsStartElement("description")))
                    {
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

                videos = list.ToArray();
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

        public static artist[] Parse(XmlReader reader)
        {
            List<artist> list = [];
            while (reader.Read() && reader.IsStartElement("artist"))
            {
                var obj = new artist();
                reader.Read();
                while (!reader.EOF)
                {
                    if (reader.Name == "artist")
                    {
                        break;
                    }

                    switch (reader.Name)
                    {
                        case "id":
                            obj.id = reader.ReadElementContentAsString();
                            break;
                        case "name":
                            obj.name = reader.ReadElementContentAsString();
                            break;
                        case "anv":
                            obj.anv = reader.ReadElementContentAsString();
                            break;
                        case "join":
                            obj.join = reader.ReadElementContentAsString();
                            break;
                        case "role":
                            obj.role = reader.ReadElementContentAsString();
                            break;
                        case "tracks":
                            obj.tracks = reader.ReadElementContentAsString();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                list.Add(obj);
            }

            return list.ToArray();
        }
    }
}
