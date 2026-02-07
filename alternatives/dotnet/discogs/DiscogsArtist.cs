namespace discogs.Artists;

[XmlType("artist")]
public class Artist : IExportable
{
    private static readonly Dictionary<string, string[]> _csvExportHeaders = new()
    {
        { "artist", "id name realname profile data_quality".Split(" ") },
        { "artist_alias", "artist_id alias_name".Split(" ") },
        { "artist_namevariation", "artist_id name".Split(" ") },
        { "artist_url", "artist_id url".Split(" ") },
        { "group_member", "group_artist_id member_artist_id member_name".Split(" ") },
        { "artist_image", "artist_id type width height".Split(" ") },
    };

    [XmlArrayItem("images")]
    public Image[] Images { get; set; }

    [XmlArray("urls")]
    [XmlArrayItem("url")]
    public string[] Urls { get; set; }

    [XmlElement("id")]
    public string Id { get; set; }

    [XmlElement("name")]
    public string Name { get; set; }

    [XmlElement("realname")]
    public string RealName { get; set; }

    [XmlElement("profile")]
    public string Profile { get; set; }

    [XmlElement("data_quality")]
    public string DataQuality { get; set; }

    [XmlArray("namevariations")]
    [XmlArrayItem("name")]
    public string[] NameVariations { get; set; }

    [XmlArray("members")]
    [XmlArrayItem("name")]
    public Name[] Members { get; set; }

    [XmlArray("aliases")]
    [XmlArrayItem("name")]
    public Name[] Aliases { get; set; }

    // groups are not parsed in the python version
    [XmlArray("groups")]
    [XmlArrayItem("name")]
    public Name[] Groups { get; set; }

    public override string ToString() => Id;

    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("artist", [Id, Name, RealName, Profile, DataQuality]);
        foreach (Name a in (Aliases ?? []))
        {
            yield return ("artist_alias", [Id, a.Value]);
        }

        foreach (string nv in (NameVariations ?? []))
        {
            yield return ("artist_namevariation", [Id, nv]);
        }

        foreach (string u in (Urls ?? []))
        {
            yield return ("artist_url", [Id, u]);
        }

        foreach (Name m in (Members ?? []))
        {
            yield return ("group_member", [Id, m.Id, m.Value]);
        }

        if ((Images?.Length ?? 0) > 0)
        {
            foreach (Image image in Images)
            {
                yield return ("artist_image", [Id, image.Type, image.Width, image.Height]);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
        => _csvExportHeaders;

    public bool IsValid() => !string.IsNullOrEmpty(Id);

    /// <summary>
    /// Populates the current object from an XML reader.
    /// </summary>
    /// <param name="reader">An XML reader positioned right after the <![CDATA[<artist>]]> node.</param>
    public void Populate(XmlReader reader) => Populate2(reader);

    private void Populate2(XmlReader reader)
    {
        if (reader.Name != "artist")
        {
            return;
        }

        // <master id="123"> unlike all others
        reader.Read();
        while (!reader.EOF)
        {
            switch (reader.Name)
            {
                case "artist":
                    // it's back on a release node (EndElement); release control
                    return;
                case "images":
                    Images = Image.Parse(reader);
                    break;
                case "id":
                    Id = reader.ReadElementContentAsString();
                    break;
                case "name":
                    Name = reader.ReadElementContentAsString();
                    break;
                case "realname":
                    RealName = reader.ReadElementContentAsString();
                    break;
                case "profile":
                    Profile = reader.ReadElementContentAsString();
                    break;
                case "data_quality":
                    DataQuality = reader.ReadElementContentAsString();
                    break;
                case "urls":
                    Urls = reader.ReadChildren("url");
                    break;
                case "namevariations":
                    NameVariations = reader.ReadChildren("name");
                    break;
                case "members":
                    Members = discogs.Artists.Name.Parse(reader, "members");
                    break;
                case "aliases":
                    Aliases = discogs.Artists.Name.Parse(reader, "aliases");
                    break;
                case "groups":
                    Groups = discogs.Artists.Name.Parse(reader, "groups");
                    break;
                default:
                    reader.Read();
                    break;
            }

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "artist")
                {
                    return;
                }

                reader.Skip();
            }
        }
    }

    private void Populate1(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.IsStartElement("artist"))
            {
                // that means we encountered the next node
                return;
            }

            if (reader.IsStartElement("id"))
            {
                Id = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("name"))
            {
                Name = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("realname"))
            {
                RealName = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("profile"))
            {
                Profile = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("data_quality"))
            {
                DataQuality = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("namevariations"))
            {
                reader.Read();
                List<string> nvs = [];
                while (reader.IsStartElement("name"))
                {
                    string nv = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(nv))
                        nvs.Add(nv);
                }

                NameVariations = nvs.ToArray();
            }

            if (reader.IsStartElement("members"))
            {
                reader.Read(); // move inside members
                List<Name> members = [];
                while (reader.IsStartElement("name") || reader.IsStartElement("id"))
                {
                    if (reader.IsStartElement("id"))
                    {
                        reader.Skip();
                        continue;
                    }

                    Name n = new()
                    {
                        Id = reader.GetAttribute("id"),
                        Value = reader.ReadElementContentAsString(),
                    };
                    members.Add(n);
                }

                this.Members = members.ToArray();
            }

            if (reader.IsStartElement("aliases"))
            {
                reader.Read();
                List<Name> aliases = [];
                while (reader.IsStartElement("name"))
                {
                    Name n = new()
                    {
                        Id = reader.GetAttribute("id"),
                        Value = reader.ReadElementContentAsString(),
                    };
                    aliases.Add(n);
                }

                this.Aliases = aliases.ToArray();
            }

            if (reader.IsStartElement("groups"))
            {
                reader.Read();
                List<Name> names = [];
                while (reader.IsStartElement("name"))
                {
                    Name n = new()
                    {
                        Id = reader.GetAttribute("id"),
                        Value = reader.ReadElementContentAsString(),
                    };
                    names.Add(n);
                }

                Groups = names.ToArray();
            }

            if (reader.IsStartElement("images"))
            {
                List<Image> images = [];
                while (reader.Read() && reader.IsStartElement("image"))
                {
                    Image image = new()
                    {
                        Type = reader.GetAttribute("type"), Width = reader.GetAttribute("width"),
                        Height = reader.GetAttribute("height")
                    };
                    images.Add(image);
                }

                this.Images = images.ToArray();
            }

            if (reader.IsStartElement("urls"))
            {
                reader.Read();
                List<string> urls = [];
                while (reader.IsStartElement("url"))
                {
                    string url = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(url))
                        urls.Add(url);
                }

                this.Urls = urls.ToArray();
            }
        }
    }
}

// [XmlRoot("name")]
[XmlType("name")]
public class Name
{
    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlText] public string Value { get; set; }

    public static Name[] Parse(XmlReader reader, string parentName)
    {
        if (reader.IsEmptyElement)
        {
            reader.Skip();
            return [];
        }

        // expects to be on <parentName> node
        reader.Read();
        List<Name> list = [];
        while (!reader.EOF)
        {
            if (reader.Name == parentName)
            {
                break;
            }

            if (reader.Name == "name")
            {
                Name obj = new()
                {
                    Id = reader.GetAttribute("id"),
                    Value = reader.ReadElementContentAsString(),
                };
                list.Add(obj);
            }
            else
            {
                reader.Skip();
            }
        }

        if (reader.NodeType == XmlNodeType.EndElement)
        {
            reader.Skip();
        }

        return list.ToArray();
    }
}
