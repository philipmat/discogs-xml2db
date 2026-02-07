namespace discogs.Labels;

[XmlType("label")]
public class Label : IExportable
{
    private static readonly Dictionary<string, string[]> _csvExportHeaders = new()
    {
        ["label"] = ["id", "name", "contact_info", "profile", "parent_name", "data_quality"],
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

    // [XmlElement( "parentLabel")]
    public ParentLabel ParentLabel { get; set; }

    [XmlArray("urls")]
    [XmlArrayItem("url")]
    public string[] Urls { get; set; }

    [XmlAttribute("id")]
    public string SubId { get; set; }

    [XmlText]
    public string SubName { get; set; }

    [XmlArray( "sublabels" )]
    public Label[] Sublabels { get; set; }

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
        yield return ("label", [Id, Name, ContactInfo, Profile, ParentLabel?.name, DataQuality]);
        if ((Urls?.Length ?? 0) > 0)
        {
            foreach (var url in Urls)
            {
                if (string.IsNullOrEmpty(url)) continue;
                yield return ("label_url", [Id, url]);
            }
        }

        if ((Images?.Length ?? 0) > 0)
        {
            foreach (var image in Images)
            {
                yield return ("label_image", [Id, image.Type, image.Width, image.Height]);
            }
        }
    }

    public void Populate(XmlReader reader) => Populate2(reader);


    public void Populate2(XmlReader reader)
    {
        if (reader.Name != "label")
        {
            return;
        }

        // <master id="123"> unlike all others
        reader.Read();
        while (!reader.EOF)
        {
            switch (reader.Name)
            {
                case "label":
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
                    ParentLabel = new ParentLabel
                    {
                        id = reader.GetAttribute("id"),
                        name = reader.ReadElementContentAsString()
                    };
                    break;
                case "sublabels":
                    Sublabels = ParseSublabels(reader);
                    break;
                default:
                    reader.Read();
                    break;
            }

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.Name == "label")
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
            if (reader.IsStartElement("label"))
            {
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

            if (reader.IsStartElement("contactinfo"))
            {
                ContactInfo = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("profile"))
            {
                Profile = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("data_quality"))
            {
                DataQuality = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("parentLabel"))
            {
                ParentLabel = new ParentLabel
                {
                    id = reader.GetAttribute("id"),
                    name = reader.ReadElementContentAsString()
                };
            }

            if (reader.IsStartElement("sublabels"))
            {
                reader.Read();
                var sublabelList = new List<Label>();
                while (reader.IsStartElement("label"))
                {
                    var label = new Label
                    {
                        Id = reader.GetAttribute("id"),
                        Name = reader.ReadElementContentAsString()
                    };
                    sublabelList.Add(label);
                }

                Sublabels = sublabelList.ToArray();
            }

            if (reader.IsStartElement("images"))
            {
                var images = new List<Image>();
                while (reader.Read() && reader.IsStartElement("image"))
                {
                    var image = new Image
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
                var urls = new List<string>();
                while (reader.IsStartElement("url"))
                {
                    var url = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(url))
                        urls.Add(url);
                }

                this.Urls = urls.ToArray();
            }
        }
    }

    public bool IsValid() => !string.IsNullOrEmpty(Id);

    private static Label[] ParseSublabels(XmlReader reader)
    {
        reader.Read();
        var sublabelList = new List<Label>();
        while (reader.IsStartElement("label"))
        {
            if (reader.IsEmptyElement)
            {
                reader.Skip();
                continue;
            }

            var label = new Label
            {
                Id = reader.GetAttribute("id"),
                Name = reader.ReadElementContentAsString()
            };
            sublabelList.Add(label);
        }

        return sublabelList.ToArray();
    }

}

[XmlType( "parentLabel" )]
public class ParentLabel
    {
        [XmlAttribute]
        public string id { get; set; }

        [XmlText] public string name { get; set; }
    }
