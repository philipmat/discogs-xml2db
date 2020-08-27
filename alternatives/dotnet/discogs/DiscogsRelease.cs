using System.Collections.Generic;
using System.Xml.Serialization;

namespace discogs.Releases
{
    public class release : IExportToCsv
    {

        [XmlAttribute]
        public string id { get; set; }

        [XmlAttribute]
        public string status { get; set; }
        public image[] images { get; set; }
        public artist[] artists { get; set; }
        public string title { get; set; }
        public label[] labels { get; set; }
        public artist[] extraartists { get; set; }
        public format[] formats { get; set; }
        [XmlArrayItem("genre")]
        public string[] genres { get; set; }
        [XmlArrayItem("style")]
        public string[] styles { get; set; }
        public string country { get; set; }
        public string released { get; set; }
        public string notes { get; set; }
        public string data_quality { get; set; }
        // has is_main_release="true,false" attribute
        public string master_id { get; set; }
        public track[] tracklist { get; set; }
        public identifier[] identifiers { get; set; }
        public video[] videos { get; set; }
        public company[] companies { get; set; }

        public IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv()
        {
            throw new System.NotImplementedException();
        }

        public IReadOnlyDictionary<string, string[]> GetCsvExportScheme()
        {
            throw new System.NotImplementedException();
        }
    }

    public class artist
    {
        public string id { get; set; }
        public string name { get; set; }
        /// <summary>Artist name variation</summary>
        public string anv { get; set; }
        public string join { get; set; }
        public string role { get; set; }
        public string tracks { get; set; }
    }

    public class label
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public string catno { get; set; }
        [XmlAttribute]
        public string id { get; set; }
    }

    public class format
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public string qty { get; set; }
        [XmlAttribute]
        public string text { get; set; }
        [XmlArrayItem("description")]
        public string[] descriptions { get; set; }
    }

    public class track
    {
        public string position { get; set; }
        public string title { get; set; }
        public string duration { get; set; }
        public artist[] artists { get; set; }
    }
    public class identifier
    {
        [XmlAttribute]
        public string type { get; set; }
        [XmlAttribute]
        public string value { get; set; }
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
    public class company
    {
        public string id { get; set; }
        public string name { get; set; }
        public string catno { get; set; }
        public string entity_type { get; set; }
        public string entity_type_name { get; set; }
        public string resource_url { get; set; }
    }
}