namespace discogs;

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
    /// <param name="reader">An XML reader positioned on the <![CDATA[<artist>]]> start element; it is left after the matching end tag.</param>
    public void Populate(XmlReader reader)
    {
        if (reader.Name != "artist")
        {
            return;
        }

        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            switch (reader.Name)
            {
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
                    Members = discogs.Name.Parse(reader);
                    break;
                case "aliases":
                    Aliases = discogs.Name.Parse(reader);
                    break;
                case "groups":
                    Groups = discogs.Name.Parse(reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (string.IsNullOrEmpty(Name))
        {
            Name = $"[artist #{Id}]";
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

    /// <summary>Reads the <c>&lt;name id="..."&gt;</c> children of the current element, ignoring others.</summary>
    public static Name[] Parse(XmlReader reader)
    {
        List<Name> list = [];
        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            if (reader.Name != "name")
            {
                reader.Skip();
                continue;
            }

            list.Add(new Name
            {
                Id = reader.GetAttribute("id"),
                Value = reader.ReadElementContentAsString(),
            });
        }

        return list.ToArray();
    }
}
