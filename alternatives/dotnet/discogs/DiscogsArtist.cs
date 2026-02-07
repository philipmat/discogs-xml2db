namespace discogs.Artists;

[XmlRoot("artist")]
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

    public image[] images { get; set; }
    [XmlArrayItem("url")]
    public string[] urls { get; set; }

    public string id { get; set; }
    public string name { get; set; }
    public string realname { get; set; }
    public string profile { get; set; }
    public string data_quality { get; set; }
    [XmlArrayItem("name")]
    public string[] namevariations { get; set; }
    public name[] members { get; set; }
    public name[] aliases { get; set; }
    // groups are not parsed in the python version
    public name[] groups {get;set;}

    public override string ToString() => id;

    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("artist", [id, name, realname, profile, data_quality]);
        foreach (var a in (aliases ?? []))
        {
            yield return ("artist_alias", [id, a.value]);
        }
        foreach (string nv in (namevariations ?? []))
        {
            yield return ("artist_namevariation", [id, nv]);
        }
        foreach (string u in (urls ?? []))
        {
            yield return ("artist_url", [id, u]);
        }
        foreach (var m in (members ?? []))
        {
            yield return ("group_member", [id, m.id, m.value]);
        }
        if ((images?.Length ?? 0) > 0)
        {
            foreach (var image in images)
            {
                yield return ("artist_image", [id, image.type, image.width, image.height]);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
        => _csvExportHeaders;

    public bool IsValid() => !string.IsNullOrEmpty(id);

    /// <summary>
    /// Populates the current object from an XML reader.
    /// </summary>
    /// <param name="reader">An XML reader positioned right after the <![CDATA[<artist>]]> node.</param>
    public void Populate(XmlReader reader) => Populate2(reader);

    public void Populate2(XmlReader reader)
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
                    images = image.Parse(reader);
                    break;
                case "id":
                    id = reader.ReadElementContentAsString();
                    break;
                case "name":
                    name = reader.ReadElementContentAsString();
                    break;
                case "realname":
                    realname = reader.ReadElementContentAsString();
                    break;
                case "profile":
                    profile = reader.ReadElementContentAsString();
                    break;
                case "data_quality":
                    data_quality = reader.ReadElementContentAsString();
                    break;
                case "urls":
                    urls = reader.ReadChildren("url");
                    break;
                case "namevariations":
                    namevariations = reader.ReadChildren("name");
                    break;
                case "members":
                    members = discogs.Artists.name.Parse(reader, "members");
                    break;
                case "aliases":
                    aliases = discogs.Artists.name.Parse(reader, "aliases");
                    break;
                case "groups":
                    groups = discogs.Artists.name.Parse(reader, "groups");
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

    public void Populate1(XmlReader reader)
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
                id = reader.ReadElementContentAsString();
            }
            if (reader.IsStartElement("name"))
            {
                name = reader.ReadElementContentAsString();
            }
            if (reader.IsStartElement("realname"))
            {
                realname = reader.ReadElementContentAsString();
            }
            if (reader.IsStartElement("profile"))
            {
                profile = reader.ReadElementContentAsString();
            }
            if (reader.IsStartElement("data_quality"))
            {
                data_quality = reader.ReadElementContentAsString();
            }
            if (reader.IsStartElement("namevariations"))
            {
                reader.Read();
                var nvs = new List<string>();
                while (reader.IsStartElement("name"))
                {
                    string nv = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(nv))
                        nvs.Add(nv);
                }
                namevariations = nvs.ToArray();
            }
            if (reader.IsStartElement("members"))
            {
                reader.Read(); // move inside members
                var members = new List<name>();
                while (reader.IsStartElement("name") || reader.IsStartElement("id"))
                {
                    if (reader.IsStartElement("id"))
                    {
                        reader.Skip();
                        continue;
                    }
                    var n = new name
                    {
                        id = reader.GetAttribute("id"),
                        value = reader.ReadElementContentAsString(),
                    };
                    members.Add(n);
                }
                this.members = members.ToArray();
            }
            if (reader.IsStartElement("aliases"))
            {
                reader.Read();
                var aliases = new List<name>();
                while (reader.IsStartElement("name"))
                {
                    var n = new name
                    {
                        id = reader.GetAttribute("id"),
                        value = reader.ReadElementContentAsString(),
                    };
                    aliases.Add(n);
                }
                this.aliases = aliases.ToArray();
            }
            if (reader.IsStartElement("groups"))
            {
                reader.Read();
                var names = new List<name>();
                while (reader.IsStartElement("name"))
                {
                    var n = new name
                    {
                        id = reader.GetAttribute("id"),
                        value = reader.ReadElementContentAsString(),
                    };
                    names.Add(n);
                }
                groups = names.ToArray();
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
            if (reader.IsStartElement("urls"))
            {
                reader.Read();
                var urls = new List<string>();
                while (reader.IsStartElement("url"))
                {
                    string url = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(url))
                        urls.Add(url);
                }
                this.urls = urls.ToArray();
            }
        }
    }
}

public class name
{
    [XmlAttribute]
    public string id { get; set; }
    [XmlText]
    public string value { get; set; }

    public static name[] Parse(XmlReader reader, string parentName)
    {
        if (reader.IsEmptyElement) {
            reader.Skip();
            return [];
        }
        // expects to be on <parentName> node
        reader.Read();
        var list = new List<name>();
        while (!reader.EOF)
        {
            if (reader.Name == parentName) {
                break;
            }
            if (reader.Name == "name") {
                var obj = new name {
                    id = reader.GetAttribute("id"),
                    value = reader.ReadElementContentAsString(),
                };
                list.Add(obj);
            }
            else {
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
