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

    public void Populate(XmlReader reader)
    {
        if (reader.Name != "master")
        {
            return;
        }

        // <master id="123"> unlike all others
        Id = reader.GetAttribute("id");
        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            switch (reader.Name)
            {
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
                    reader.Skip();
                    break;
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
            int depth = reader.EnterElement();
            while (reader.NextChildElement(depth))
            {
                if (reader.Name != "artist")
                {
                    reader.Skip();
                    continue;
                }

                Artist obj = new();
                list.Add(obj);
                int artistDepth = reader.EnterElement();
                while (reader.NextChildElement(artistDepth))
                {
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
            }

            return list.ToArray();
        }
    }
}
