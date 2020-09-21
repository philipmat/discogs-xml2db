using System.Collections.Generic;
using System.Xml.Serialization;

namespace discogs
{
    public interface IExportToCsv
    {
        /// <summary>
        /// Returns the names of all files an implementation would produce and their headers.
        /// </summary>
        /// <returns>A dictionary where the key is the file name (e.g. "artist_url") and the
        /// value is an array of headers (e.g. [ "url_id", "url", "artist_id" ].</returns>
        IReadOnlyDictionary<string, string[]> GetCsvExportScheme();

        /// <summary>
        /// Produces list of tuples where the StreamName is the kind of record, matching
        /// the key from <see cref="GetCsvExportScheme"/>, and the RowValues contains
        /// one entry for each of the columns the csv file has.
        /// </summary>
        /// <returns>Ex: <code>[ (StreamName: "artist_url", RowValues: ["1", "1", "http://example.com"]), ...]</code></returns>
        IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv();
    }

    public class image
    {
        [XmlAttribute]
        public string type { get; set; }
        [XmlAttribute]
        public string uri { get; set; }
        [XmlAttribute]
        public string uri150 { get; set; }
        [XmlAttribute]
        public string width { get; set; }
        [XmlAttribute]
        public string height { get; set; }
    }

    public class url
    {
        [XmlElement("url")]
        public string TheUrl { get; set; }
    }
    public class video
    {
        [XmlAttribute]
        public string src { get; set; }
        [XmlAttribute]
        public string duration { get; set; }
        [XmlAttribute]
        public string embed { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }
}