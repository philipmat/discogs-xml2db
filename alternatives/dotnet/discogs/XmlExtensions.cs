
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace discogs
{
    public static class XmlExtensions
    {
        public static string[] ReadChildren(this XmlReader reader, string childName)
        {
            // expects reader to be positions on parent node
            reader.Read();

            var list = new List<string>();
            while(reader.IsStartElement(childName))
            {
                var e = reader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(e))
                    list.Add(e);
            }

            return list.ToArray();
        }
    }
}
