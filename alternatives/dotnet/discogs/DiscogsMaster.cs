namespace discogs;

[XmlType("master")]
public class Master : IExportable
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

    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlElement( "main_release")]
    public string MainRelease { get; set; }
    [XmlElement( "year")]
    public string Year { get; set; }
    [XmlElement( "title")]
    public string Title { get; set; }
    [XmlElement( "data_quality")]
    public string DataQuality { get; set; }
    [XmlArray("images")]
    public Image[] Images { get; set; }
    [XmlArray("artists")]
    public Artist[] Artists { get; set; }

    [XmlArray("genres")]
    [XmlArrayItem("genre")]
    public string[] Genres { get; set; }

    [XmlArray("styles")]
    [XmlArrayItem("style")]
    public string[] Styles { get; set; }

    [XmlArray("videos")]
    public Video[] Videos { get; set; }

    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("master", [Id, Title, Year, MainRelease, DataQuality]);
        if (Artists?.Length > 0)
        {
            int position = 1;
            foreach (Artist a in Artists)
            {
                if (a == null) continue;
                yield return ("master_artist",
                    [Id, a.Id, a.Name, a.ArtistNameVariation, (position++).ToString(), a.Join, a.Role /*, a.tracks*/]);
            }
        }

        if (Videos?.Length > 0)
        {
            foreach (Video v in Videos)
            {
                if (v == null) continue;
                yield return ("master_video", [Id, v.Duration, v.Title, v.Description, v.Src]);
            }
        }

        if (Genres?.Length > 0)
        {
            foreach (string g in Genres)
            {
                if (string.IsNullOrEmpty(g)) continue;
                yield return ("master_genre", [Id, g]);
            }
        }

        if (Styles?.Length > 0)
        {
            foreach (string s in Styles)
            {
                if (string.IsNullOrEmpty(s)) continue;
                yield return ("master_style", [Id, s]);
            }
        }

        if (Images?.Length > 0)
        {
            foreach (Image image in Images)
            {
                yield return ("master_image", [Id, image.Type, image.Width, image.Height]);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => _csvExportHeaders;

    public bool IsValid() => !string.IsNullOrEmpty(Id);

    public void Populate(XmlReader reader) => Populate2(reader);


    public void Populate2(XmlReader reader)
    {
        if (reader.Name != "master")
        {
            return;
        }

        // <master id="123"> unlike all others
        Id = reader.GetAttribute("id");
        reader.Read();
        while (!reader.EOF)
        {
            switch (reader.Name)
            {
                case "master":
                    // it's back on a master node (EndElement); release control
                    return;
                case "main_release":
                    MainRelease = reader.ReadElementContentAsString();
                    break;
                case "year":
                    Year = reader.ReadElementContentAsString();
                    break;
                case "title":
                    Title = reader.ReadElementContentAsString();
                    break;
                case "data_quality":
                    DataQuality = reader.ReadElementContentAsString();
                    break;
                case "images":
                    Images = Image.Parse(reader);
                    break;
                case "genres":
                    Genres = reader.ReadChildren("genre");
                    break;
                case "styles":
                    Styles = reader.ReadChildren("style");
                    break;
                case "videos":
                    Videos = Video.Parse(reader);
                    break;
                case "artists":
                    Artists = Artist.Parse(reader);
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
        Id = reader.GetAttribute("id");
        while (reader.Read())
        {
            if (reader.IsStartElement("master"))
            {
                // that means we encountered the next node
                return;
            }

            if (reader.IsStartElement("main_release"))
            {
                MainRelease = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("images"))
            {
                List<Image> imageList = [];
                while (reader.Read() && reader.IsStartElement("image"))
                {
                    Image image = new Image
                    {
                        Type = reader.GetAttribute("type"), Width = reader.GetAttribute("width"),
                        Height = reader.GetAttribute("height")
                    };
                    imageList.Add(image);
                }

                this.Images = imageList.ToArray();
            }

            if (reader.IsStartElement("artists"))
            {
                List<Artist> list = [];
                reader.Read();
                while (reader.IsStartElement("artist"))
                {
                    Artist artist = new Artist();
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
                                artist.Id = reader.ReadElementContentAsString();
                            if (reader.IsStartElement("name"))
                                artist.Name = reader.ReadElementContentAsString();
                            if (reader.IsStartElement("anv"))
                                artist.ArtistNameVariation = reader.ReadElementContentAsString();
                            if (reader.IsStartElement("join"))
                                artist.Join = reader.ReadElementContentAsString();
                            if (reader.IsStartElement("role"))
                                artist.Role = reader.ReadElementContentAsString();
                            if (reader.IsStartElement("tracks"))
                                artist.Tracks = reader.ReadElementContentAsString();
                        }
                    }

                    list.Add(artist);
                    if (!reader.IsStartElement("artist"))
                    {
                        reader.ReadEndElement();
                    }
                }

                Artists = list.ToArray();
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

                Genres = list.ToArray();
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

                Styles = list.ToArray();
            }

            if (reader.IsStartElement("year"))
            {
                Year = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("title"))
            {
                Title = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("data_quality"))
            {
                DataQuality = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("videos"))
            {
                List<Video> list = [];
                reader.Read();
                while (reader.IsStartElement("video"))
                {
                    Video video = new Video
                    {
                        Src = reader.GetAttribute("src"),
                        Duration = reader.GetAttribute("duration"),
                        Embed = reader.GetAttribute("embed"),
                    };
                    while (reader.Read()
                           && (reader.IsStartElement("title") || reader.IsStartElement("description")))
                    {
                        if (reader.IsStartElement("title"))
                        {
                            video.Title = reader.ReadElementContentAsString();
                        }

                        if (reader.IsStartElement("description"))
                        {
                            video.Description = reader.ReadElementContentAsString();
                        }
                    }

                    list.Add(video);
                    if (!reader.IsStartElement("video"))
                    {
                        reader.ReadEndElement();
                    }
                }

                Videos = list.ToArray();
            }
        }
    }

    [XmlType("artist")]
    public class Artist
    {
        [XmlElement("id")]
        public string Id { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }

        /// <summary>Artist name variation</summary>
        [XmlElement( "anv" )]
        public string ArtistNameVariation { get; set; }

        [XmlElement("join")]
        public string Join { get; set; }
        [XmlElement("role")]
        public string Role { get; set; }
        [XmlElement("tracks")]
        public string Tracks { get; set; }

        public static Artist[] Parse(XmlReader reader)
        {
            List<Artist> list = [];
            while (reader.Read() && reader.IsStartElement("artist"))
            {
                Artist obj = new Artist();
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
                            obj.Id = reader.ReadElementContentAsString();
                            break;
                        case "name":
                            obj.Name = reader.ReadElementContentAsString();
                            break;
                        case "anv":
                            obj.ArtistNameVariation = reader.ReadElementContentAsString();
                            break;
                        case "join":
                            obj.Join = reader.ReadElementContentAsString();
                            break;
                        case "role":
                            obj.Role = reader.ReadElementContentAsString();
                            break;
                        case "tracks":
                            obj.Tracks = reader.ReadElementContentAsString();
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
