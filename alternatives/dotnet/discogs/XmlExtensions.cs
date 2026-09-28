namespace discogs;

/// <summary>
/// Depth-based navigation that treats <c>&lt;x/&gt;</c> exactly like <c>&lt;x&gt;&lt;/x&gt;</c>.
/// Usage, with the reader positioned on a start element:
/// <code>
/// int depth = reader.EnterElement();
/// while (reader.NextChildElement(depth))
/// {
///     switch (reader.Name)
///     {
///         case "title": Title = reader.ReadElementContentAsString(); break;
///         default: reader.Skip(); break;
///     }
/// }
/// </code>
/// Every child handler must consume the whole child element (<c>ReadElementContentAsString</c>, <c>Skip</c>,
/// or a nested Enter/Next loop). When the loop ends the reader is positioned after the parent's end tag.
/// </summary>
public static class XmlExtensions
{
    private const int NoChildren = -1;

    extension(XmlReader reader)
    {
        /// <summary>
        /// Moves into the current element and returns its depth, to be passed to <see cref="NextChildElement"/>.
        /// A self-closing element is consumed right away.
        /// </summary>
        public int EnterElement()
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return NoChildren;
            }

            int depth = reader.Depth;
            reader.Read();
            return depth;
        }

        /// <summary>
        /// Moves to the next child element of the element entered at <paramref name="parentDepth"/>.
        /// Returns false, positioned after the parent's end tag, when there are no more children.
        /// </summary>
        public bool NextChildElement(int parentDepth)
        {
            if (parentDepth == NoChildren)
            {
                return false;
            }

            while (!reader.EOF)
            {
                if (reader.Depth <= parentDepth)
                {
                    // the parent's EndElement
                    reader.Read();
                    return false;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Depth == parentDepth + 1)
                {
                    return true;
                }

                reader.Read();
            }

            return false;
        }

        /// <summary>Reads the non-blank text of every <paramref name="childName"/> child of the current element.</summary>
        public string[] ReadChildren(string childName)
        {
            var list = new List<string>();
            int depth = reader.EnterElement();
            while (reader.NextChildElement(depth))
            {
                if (reader.Name != childName)
                {
                    reader.Skip();
                    continue;
                }

                string e = reader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(e))
                    list.Add(e);
            }

            return list.ToArray();
        }
    }
}
