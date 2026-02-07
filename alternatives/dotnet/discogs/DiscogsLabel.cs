namespace discogs.Labels;

public class label : IExportable
{
    private static readonly Dictionary<string, string[]> CsvExportHeaders = new()
    {
        ["label"] = ["id", "name", "contact_info", "profile", "parent_name", "data_quality"],
        ["label_url"] = ["label_id", "url"],
        ["label_image"] = ["label_id", "type", "width", "height"],
    };

    public image[] images { get; set; }
    public string id { get; set; }
    public string name { get; set; }

    private string contactinfo;


    public string profile { get; set; }
    public string data_quality { get; set; }
    public ParentLabel parentLabel { get; set; }

    [XmlArrayItem("url")]
    public string[] urls { get; set; }

    [XmlAttribute("id")]
    public string SubId { get; set; }

    [XmlText] public string SubName { get; set; }

    public label[] sublabels { get; set; }

    /// <summary>
    /// Gets the possible export schemes for the class
    /// </summary>
    /// <returns>A read-only dictionary where the key is the type of export stream and the values are the headers/columns/fields exported.</returns>
    public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields() => CsvExportHeaders;

    /// <summary>
    /// Exports instance to CSV.
    /// </summary>
    /// <returns>Tuples where the StreamName matches a key from <see ref="GetCsvExportScheme"> </returns>
    public IEnumerable<(string StreamName, string[] RowValues)> Export()
    {
        yield return ("label", [id, name, contactinfo, profile, parentLabel?.name, data_quality]);
        if ((urls?.Length ?? 0) > 0)
        {
            foreach (var url in urls)
            {
                if (string.IsNullOrEmpty(url)) continue;
                yield return ("label_url", [id, url]);
            }
        }

        if ((images?.Length ?? 0) > 0)
        {
            foreach (var image in images)
            {
                yield return ("label_image", [id, image.type, image.width, image.height]);
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
                    images = image.Parse(reader);
                    break;
                case "id":
                    id = reader.ReadElementContentAsString();
                    break;
                case "name":
                    name = reader.ReadElementContentAsString();
                    break;
                case "contactinfo":
                    contactinfo = reader.ReadElementContentAsString();
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
                case "parentLabel":
                    parentLabel = new ParentLabel
                    {
                        id = reader.GetAttribute("id"),
                        name = reader.ReadElementContentAsString()
                    };
                    break;
                case "sublabels":
                    sublabels = ParseSublabels(reader);
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
                id = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("name"))
            {
                name = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("contactinfo"))
            {
                contactinfo = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("profile"))
            {
                profile = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("data_quality"))
            {
                data_quality = reader.ReadElementContentAsString();
            }

            if (reader.IsStartElement("parentLabel"))
            {
                parentLabel = new ParentLabel
                {
                    id = reader.GetAttribute("id"),
                    name = reader.ReadElementContentAsString()
                };
            }

            if (reader.IsStartElement("sublabels"))
            {
                reader.Read();
                var sublabelList = new List<label>();
                while (reader.IsStartElement("label"))
                {
                    var label = new label
                    {
                        id = reader.GetAttribute("id"),
                        name = reader.ReadElementContentAsString()
                    };
                    sublabelList.Add(label);
                }

                sublabels = sublabelList.ToArray();
            }

            if (reader.IsStartElement("images"))
            {
                var images = new List<image>();
                while (reader.Read() && reader.IsStartElement("image"))
                {
                    var image = new image
                    {
                        type = reader.GetAttribute("type"), width = reader.GetAttribute("width"),
                        height = reader.GetAttribute("height")
                    };
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
                    var url = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(url))
                        urls.Add(url);
                }

                this.urls = urls.ToArray();
            }
        }
    }

    public bool IsValid() => !string.IsNullOrEmpty(id);

    private static label[] ParseSublabels(XmlReader reader)
    {
        reader.Read();
        var sublabelList = new List<label>();
        while (reader.IsStartElement("label"))
        {
            if (reader.IsEmptyElement)
            {
                reader.Skip();
                continue;
            }

            var label = new label
            {
                id = reader.GetAttribute("id"),
                name = reader.ReadElementContentAsString()
            };
            sublabelList.Add(label);
        }

        return sublabelList.ToArray();
    }


    public class ParentLabel
    {
        [XmlAttribute]
        public string id { get; set; }

        [XmlText] public string name { get; set; }
    }
}
