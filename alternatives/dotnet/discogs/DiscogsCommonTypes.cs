namespace discogs;

[XmlType("image")]
public class Image
{
    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("uri")]
    public string Uri { get; set; }

    [XmlAttribute("uri150")]
    public string Uri150 { get; set; }

    [XmlAttribute("width")]
    public string Width { get; set; }

    [XmlAttribute("height")]
    public string Height { get; set; }

    internal static Image[] Parse(XmlReader reader)
    {
        List<Image> list = [];
        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            if (reader.Name == "image")
            {
                list.Add(ParseImage(reader));
            }

            reader.Skip();
        }

        return list.ToArray();
    }

    internal static Image ParseImage(XmlReader reader)
        => new()
        {
            Type = reader.GetAttribute("type"),
            Width = reader.GetAttribute("width"),
            Height = reader.GetAttribute("height")
        };
}

[XmlType("url")]
public class Url
{
    [XmlElement("url")]
    public string TheUrl { get; set; }
}

[XmlRoot("video")]
public class Video
{
    [XmlAttribute("src")]
    public string Src { get; set; }

    [XmlAttribute("duration")]
    public string Duration { get; set; }

    [XmlAttribute("embed")]
    public string Embed { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }
    [XmlElement("description")]
    public string Description { get; set; }

    internal static Video[] Parse(XmlReader reader)
    {
        List<Video> list = [];
        int depth = reader.EnterElement();
        while (reader.NextChildElement(depth))
        {
            if (reader.Name != "video")
            {
                reader.Skip();
                continue;
            }

            Video one = new()
            {
                Src = reader.GetAttribute("src"),
                Duration = reader.GetAttribute("duration"),
                Embed = reader.GetAttribute("embed"),
            };
            list.Add(one);

            int videoDepth = reader.EnterElement();
            while (reader.NextChildElement(videoDepth))
            {
                switch (reader.Name)
                {
                    case "title":
                        one.Title = reader.ReadElementContentAsString();
                        break;
                    case "description":
                        one.Description = reader.ReadElementContentAsString();
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
