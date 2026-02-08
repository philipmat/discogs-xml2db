namespace discogs;

[XmlType("release")]
public class Release : IExportable
{
    private static readonly Dictionary<string, string[]> _csvExportHeaders = new()
    {
        { "release", "id title released country notes data_quality master_id status".Split(" ") },
        { "release_genre", "release_id genre".Split(" ") },
        { "release_label", "release_id label_id label_name catno".Split(" ") },
        { "release_style", "release_id style".Split(" ") },
        { "release_image", "release_id type width height".Split(" ") },
        { "release_format", "release_id name qty text_string descriptions".Split(" ") },
        { "release_identifier", "release_id description type value".Split(" ") },
        { "release_company", "release_id company_id company_name entity_type entity_type_name uri".Split(" ") },
        { "release_video", "release_id duration title description uri".Split(" ") },
        { "release_artist", "release_id artist_id artist_name extra anv position join_string role tracks".Split(" ") },
        { "release_track", "release_id sequence position parent title duration track_id".Split(" ") },
        {
            "release_track_artist",
            "release_id track_sequence track_id artist_id artist_name extra anv position join_string role tracks".Split(
                " ")
        },
    };

    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlAttribute("status")]
    public string Status { get; set; }

    [XmlArrayItem("images")]
    public Image[] Images { get; set; }

    [XmlArray("artists")]
    public Artist[] Artists { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }
    [XmlArray("labels")]
    public Label[] Labels { get; set; }
    [XmlArray("extraartists")]
    public Artist[] ExtraArtists { get; set; }
    [XmlArray("formats")]
    public Format[] Formats { get; set; }

    [XmlArray( "genres")]
    [XmlArrayItem("genre")]
    public string[] Genres { get; set; }

    [XmlArray( "styles")]
    [XmlArrayItem("style")]
    public string[] Styles { get; set; }

    [XmlElement( "country")]
    public string Country { get; set; }
    [XmlElement( "released")]
    public string Released { get; set; }
    [XmlElement( "notes")]
    public string Notes { get; set; }

    [XmlElement( "data_quality")]
    public string DataQuality { get; set; }

    // has is_main_release="true,false" attribute
    [XmlElement( "master_id" )]
    public string MasterId { get; set; }
    [XmlArray( "tracklist")]
    public Track[] TrackList { get; set; }
    [XmlArray( "identifiers")]
    public Identifier[] Identifiers { get; set; }
    [XmlArray( "videos")]
    public Video[] Videos { get; set; }
    [XmlArray( "companies")]
    public Company[] Companies { get; set; }

    private IEnumerable<Track> GetTracks()
    {
        if ((TrackList?.Length ?? 0) == 0)
        {
            yield break;
        }

        for (int i = 0; i < TrackList.Length; i++)
        {
            TrackList[i].SetTrackId(Id, i + 1);
            yield return TrackList[i];
            if ((TrackList[i].SubTracks?.Length ?? 0) > 0)
            {
                for (int j = 0; j < TrackList[i].SubTracks.Length; j++)
                {
                    TrackList[i].SubTracks[j].SetTrackId(Id, i + 1, j + 1);
                    yield return TrackList[i].SubTracks[j];
                }
            }
        }
    }

    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("release", [Id, Title, Released, Country, Notes, DataQuality, MasterId, Status]);
        if (Genres?.Length > 0)
        {
            foreach (string g in Genres)
            {
                if (string.IsNullOrEmpty(g)) continue;
                yield return ("release_genre", [Id, g]);
            }
        }

        if (Labels?.Length > 0)
        {
            foreach (Label l in Labels)
            {
                if (l == null) continue;
                yield return ("release_label", [Id, l.Id, l.Name, l.CatalogNumber]);
            }
        }

        if (Styles?.Length > 0)
        {
            foreach (string s in Styles)
            {
                if (string.IsNullOrEmpty(s)) continue;
                yield return ("release_style", [Id, s]);
            }
        }

        if (Images?.Length > 0)
        {
            foreach (Image image in Images)
            {
                yield return ("release_image", [Id, image.Type, image.Width, image.Height]);
            }
        }

        if (Formats?.Length > 0)
        {
            foreach (Format f in Formats)
            {
                if (f == null) continue;
                yield return ("release_format",
                [
                    Id, f.Name, f.Quantity, f.Text, string.Join("; ", f.Descriptions ?? [])
                ]);
            }
        }

        if (Identifiers?.Length > 0)
        {
            foreach (Identifier i in Identifiers)
            {
                if (i == null) continue;
                yield return ("release_identifier", [Id, i.Description, i.Type, i.Value]);
            }
        }

        if (Companies?.Length > 0)
        {
            foreach (Company c in Companies)
            {
                if (c == null) continue;
                yield return ("release_company",
                    [Id, c.Id, c.Name, c.EntityType, c.EntityTypeName, c.ResourceUrl]);
            }
        }

        if (Videos?.Length > 0)
        {
            foreach (Video v in Videos)
            {
                if (v == null) continue;
                yield return ("release_video", [Id, v.Duration, v.Title, v.Description, v.Src]);
            }
        }

        if (Artists?.Length > 0)
        {
            int position = 1;
            foreach (Artist a in Artists)
            {
                if (a == null) continue;
                yield return ("release_artist",
                    [Id, a.Id, a.Name, "0", a.ArtistNameVariation, (position++).ToString(), a.Join, a.Role, a.Tracks]);
            }
        }

        if (ExtraArtists?.Length > 0)
        {
            int position = 1;
            foreach (Artist a in ExtraArtists)
            {
                if (a == null) continue;
                yield return ("release_artist",
                    [Id, a.Id, a.Name, "1", a.ArtistNameVariation, (position++).ToString(), a.Join, a.Role, a.Tracks]);
            }
        }

        int seq = 0;
        string seqs = "";
        foreach (Track t in GetTracks())
        {
            seq += 1;
            seqs = seq.ToString();
            yield return ("release_track",
                [Id, seqs, t.Position, t.ParentTrackId, t.Title, t.Duration, t.TrackId]);
            int artistSeq = 0;
            foreach (Artist a in (t.Artists ?? []))
            {
                if (a == null) continue;
                artistSeq += 1;
                yield return ("release_track_artist",
                [
                    Id, seqs, t.TrackId, a.Id, a.Name, "0", a.ArtistNameVariation, artistSeq.ToString(), a.Join, a.Role,
                    a.Tracks
                ]);
            }

            artistSeq = 0;
            foreach (Artist a in (t.ExtraArtists ?? []))
            {
                if (a == null) continue;
                artistSeq += 1;
                yield return ("release_track_artist",
                [
                    Id, seqs, t.TrackId, a.Id, a.Name, "1", a.ArtistNameVariation, artistSeq.ToString(), a.Join, a.Role,
                    a.Tracks
                ]);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
        => _csvExportHeaders;

    public void Populate(XmlReader reader)
    {
        if (reader.Name != "release")
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
                case "release":
                    // it's back on a release node (EndElement); release control
                    return;
                case "title":
                    Title = reader.ReadElementContentAsString();
                    break;
                case "country":
                    Country = reader.ReadElementContentAsString();
                    break;
                case "released":
                    Released = reader.ReadElementContentAsString();
                    break;
                case "notes":
                    Notes = reader.ReadElementContentAsString();
                    break;
                case "data_quality":
                    DataQuality = reader.ReadElementContentAsString();
                    break;
                case "master_id":
                    MasterId = reader.ReadElementContentAsString();
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
                case "identifiers":
                    Identifiers = Identifier.Parse(reader);
                    break;
                case "labels":
                    Labels = Label.Parse(reader);
                    break;
                case "formats":
                    Formats = Format.Parse(reader);
                    break;
                case "artists":
                    Artists = Artist.Parse(reader);
                    break;
                case "extraartists":
                    ExtraArtists = Artist.Parse(reader);
                    break;
                case "companies":
                    Companies = Company.Parse(reader);
                    break;
                case "tracklist":
                    TrackList = Track.Parse(reader);
                    break;
                default:
                    reader.Read();
                    break;
            }

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "release")
                {
                    return;
                }

                reader.Skip();
            }
        }
    }

    public bool IsValid() => !string.IsNullOrEmpty(Id);


    [XmlRoot("artist")]
    public class Artist
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>Artist name variation</summary>
        [XmlAttribute("anv")]
        public string ArtistNameVariation { get; set; }

        [XmlAttribute("join")]
        public string Join { get; set; }

        [XmlAttribute("role")]
        public string Role { get; set; }

        [XmlAttribute("tracks")]
        public string Tracks { get; set; }

        public static Artist[] Parse(XmlReader reader)
        {
            List<Artist> list = [];
            while (reader.Read() && reader.IsStartElement("artist"))
            {
                Artist obj = new();
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

    [XmlRoot("label")]
    public class Label
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("catno")]
        public string CatalogNumber { get; set; }

        public static Label[] Parse(XmlReader reader)
        {
            // expects to be on the <identifiers> node
            List<Label> list = [];
            while (reader.Read() && reader.IsStartElement("label"))
            {
                Label obj = new()
                {
                    Name = reader.GetAttribute("name"),
                    CatalogNumber = reader.GetAttribute("catno"),
                    Id = reader.GetAttribute("id"),
                };
                list.Add(obj);
            }

            return list.ToArray();
        }
    }

    [XmlRoot("format")]
    public class Format
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("qty")]
        public string Quantity { get; set; }

        [XmlAttribute("test")]
        public string Text { get; set; }

        [XmlArray("descriptions")]
        [XmlArrayItem("description")]
        public string[] Descriptions { get; set; }

        public static Format[] Parse(XmlReader reader)
        {
            // expects to be on <identifiers> node
            // reader.Read();
            List<Format> list = [];
            while (reader.Read() && reader.IsStartElement("format"))
            {
                Format obj = new()
                {
                    Name = reader.GetAttribute("name"),
                    Quantity = reader.GetAttribute("qty"),
                    Text = reader.GetAttribute("text"),
                };
                // read descriptions
                if (reader.IsEmptyElement)
                {
                    // does not have descriptions
                    list.Add(obj);
                    continue;
                }

                reader.Read();
                obj.Descriptions = reader.ReadChildren("description");
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    reader.Skip();
                }

                list.Add(obj);
            }

            return list.ToArray();
        }
    }

    [XmlRoot("track")]
    public class Track
    {
        private const string TrackIdFormat = "{0}.{1}";
        private const string SubTrackIdFormat = "{0}.{1}.{2}";

        [XmlElement("position")]
        public string Position { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("duration")]
        public string Duration { get; set; }

        [XmlArray("artists")]
        public Artist[] Artists { get; set; } = [];

        [XmlArray("extraartists")]
        public Artist[] ExtraArtists { get; set; } = [];

        [XmlArray("sub_tracks")]
        public Track[] SubTracks { get; set; } = [];

        [XmlElement("track_id")]
        internal string TrackId { get; private set; } = "";

        [XmlElement("parent_track_id")]
        internal string ParentTrackId { get; private set; } = "";

        public void SetTrackId(string releaseId, int trackSeq)
            => TrackId = string.Format(TrackIdFormat, releaseId, trackSeq);

        public void SetTrackId(string releaseId, int trackSeq, int subTrackSeq)
        {
            ParentTrackId = string.Format(TrackIdFormat, releaseId, trackSeq);
            TrackId = string.Format(SubTrackIdFormat, releaseId, trackSeq, subTrackSeq);
        }

        public static Track[] Parse(XmlReader reader)
        {
            List<Track> list = new();
            while (reader.Read() && reader.IsStartElement("track"))
            {
                Track obj = new();
                reader.Read(); // mode into sub-nodes
                while (!reader.EOF)
                {
                    if (reader.Name == "track")
                    {
                        break;
                    }

                    switch (reader.Name)
                    {
                        case "position":
                            obj.Position = reader.ReadElementContentAsString();
                            break;
                        case "title":
                            obj.Title = reader.ReadElementContentAsString();
                            break;
                        case "duration":
                            obj.Duration = reader.ReadElementContentAsString();
                            break;
                        case "artists":
                            obj.Artists = Artist.Parse(reader);
                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                reader.Skip();
                            }

                            break;
                        case "extraartists":
                            obj.ExtraArtists = Artist.Parse(reader);
                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                reader.Skip();
                            }

                            break;
                        case "sub_tracks":
                            obj.SubTracks = Parse(reader);
                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                reader.Skip();
                            }

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

    [XmlRoot("identifier")]
    public class Identifier
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        public static Identifier[] Parse(XmlReader reader)
        {
            // expects to be on the < identifiers > node
            List<Identifier> list = [];
            while (reader.Read() && reader.IsStartElement("identifier"))
            {
                Identifier obj = new()
                {
                    Type = reader.GetAttribute("type"),
                    Value = reader.GetAttribute("value"),
                    Description = reader.GetAttribute("description"),
                };
                list.Add(obj);
            }

            return list.ToArray();
        }
    }

    [XmlRoot("company")]
    public class Company
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("catno")]
        public string CatalogNumber { get; set; }

        [XmlElement("entity_type")]
        public string EntityType { get; set; }

        [XmlElement("entity_type_name")]
        public string EntityTypeName { get; set; }

        [XmlElement("resource_url")]
        public string ResourceUrl { get; set; }

        public static Company[] Parse(XmlReader reader)
        {
            List<Company> list = [];
            while (reader.Read() && reader.IsStartElement("company"))
            {
                Company obj = new();
                reader.Read();
                while (!reader.EOF)
                {
                    if (reader.Name == "company")
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
                        case "catno":
                            obj.CatalogNumber = reader.ReadElementContentAsString();
                            break;
                        case "entity_type":
                            obj.EntityType = reader.ReadElementContentAsString();
                            break;
                        case "entity_type_name":
                            obj.EntityTypeName = reader.ReadElementContentAsString();
                            break;
                        case "resource_url":
                            obj.ResourceUrl = reader.ReadElementContentAsString();
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
