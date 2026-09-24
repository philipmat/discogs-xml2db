namespace discogs;

public static class XmlExtensions
{
    extension(XmlReader reader)
    {
        public string[] ReadChildren(string childName)
        {
            // expects reader to be positions on parent node
            reader.Read();

            var list = new List<string>();
            while (reader.IsStartElement(childName))
            {
                string e = reader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(e))
                    list.Add(e);
            }

            return list.ToArray();
        }
    }
}
