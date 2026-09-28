namespace discogs;

[XmlType("label")]
public class Label : IExportable
{
    private static readonly Dictionary<string, string[]> _csvExportHeaders = new()
    {
        ["label"] = ["id", "name", "contact_info", "profile", "parent_name", "parent_id", "data_quality"],
        ["label_url"] = ["label_id", "url"],
        ["label_image"] = ["label_id", "type", "width", "height"],
    };

    [XmlArrayItem("images")]
    public Image[] Images { get; set; }

    [XmlElement("id")]
    public string Id { get; set; }

    [XmlElement("name")]
    public string Name { get; set; }

    [XmlElement("contactinfo")]
    [XmlText]
    public string ContactInfo { get; set; }


    [XmlElement("profile")]
    public string Profile { get; set; }

    [XmlElement("data_quality")]
    public string DataQuality { get; set; }

    [XmlElement( "parentLabel")]
    public ParentLabel ParentLabel { get; set; }

    [XmlArray("urls")]
    [XmlArrayItem("url")]
    public string[] Urls { get; set; }
    [XmlArray("sublabels")]
    public Label[] Sublabels { get; set; }

    [Obsolete("Was used by the XmlSerializer")]
    [XmlAttribute("id")]
    public string SubId { get; set; }

    [Obsolete("Was used by the XmlSerializer")]
    [XmlText]
    public string SubName { get; set; }


    public bool IsSubLabel { get; private init; }

    /// <summary>
    /// Gets the possible export schemes for the class
    /// </summary>
    /// <returns>A read-only dictionary where the key is the type of export stream and the values are the headers/columns/fields exported.</returns>
    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => _csvExportHeaders;

    /// <summary>
    /// Exports instance to CSV.
    /// </summary>
    /// <returns>Tuples where the StreamName matches a key from <see ref="GetCsvExportScheme"> </returns>
    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("label", [Id, Name, ContactInfo, Profile, ParentLabel?.Name, ParentLabel?.Id, DataQuality]);
        if ((Urls?.Length ?? 0) > 0)
        {
            foreach (string url in Urls)
            {
                if (string.IsNullOrEmpty(url)) continue;
                yield return ("label_url", [Id, url]);
            }
        }

        if ((Images?.Length ?? 0) > 0)
        {
            foreach (Image image in Images)
            {
                yield return ("label_image", [Id, image.Type, image.Width, image.Height]);
            }
        }
    }

    public void Populate(XmlReader reader)
    {
        if (reader.Name != "label")
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
                case "contactinfo":
                    ContactInfo = reader.ReadElementContentAsString();
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
                case "parentLabel":
                    ParentLabel = new()
                    {
                        Id = reader.GetAttribute("id"),
                        Name = reader.ReadElementContentAsString()
                    };
                    break;
                case "sublabels":
                    Sublabels = ParseSublabels(reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }
    }

    public bool IsValid() => !string.IsNullOrEmpty(Id);

    private static Label[] ParseSublabels(XmlReader reader)
    {
        List<Label> sublabelList = [];
        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            if (reader.Name != "label")
            {
                reader.Skip();
                continue;
            }

            Label label = new()
            {
                Id = reader.GetAttribute("id"),
                Name = reader.ReadElementContentAsString(),
                IsSubLabel = true
            };
            // <label id="1"/> and <label id="1"></label> carry no sublabel name
            if (!string.IsNullOrEmpty(label.Name))
            {
                sublabelList.Add(label);
            }
        }

        return sublabelList.ToArray();
    }
}

[XmlType("parentLabel")]
public class ParentLabel
{
    [XmlAttribute("id")]
    public string Id { get; set; }

    [XmlText] public string Name { get; set; }
}
