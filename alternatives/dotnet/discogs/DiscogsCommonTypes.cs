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
        while (reader.Read() && reader.IsStartElement("image"))
        {
            Image obj = ParseImage(reader);
            list.Add(obj);
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
        while (reader.Read() && reader.IsStartElement("video"))
        {
            Video one = new()
            {
                Src = reader.GetAttribute("src"),
                Duration = reader.GetAttribute("duration"),
                Embed = reader.GetAttribute("embed"),
            };

            reader.Read();
            while (!reader.EOF)
            {
                if (reader.Name == "title")
                {
                    one.Title = reader.ReadElementContentAsString();
                    continue;
                }

                if (reader.Name == "description")
                {
                    one.Description = reader.ReadElementContentAsString();
                    continue;
                }

                if (reader.Name == "video")
                {
                    // reader.Skip();
                    break;
                }

                // any other element
                reader.Read();
            }

            list.Add(one);
        }

        return list.ToArray();
    }
}
