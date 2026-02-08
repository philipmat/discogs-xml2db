namespace discogs;

/// <summary>
/// Represents an interface for objects that can be exported for use in external systems or formats.
/// </summary>
public interface IExportable
{
    /// <summary>
    /// Reports how many different collections or groups of data this one object can produce.
    /// For example, an artist can produce:
    /// * artist - the main information about an artist
    /// * artist_alias - aliases for an artist
    /// * artist_namevariation - variations of an artist's name
    /// * artist_url - all urls for an artist
    /// * group_member - members of a group, if an artist is a group
    /// * artist_image - images for an artist.
    ///
    /// Each of these categories would effectively translate to a csv file
    /// or a table, etc.
    /// </summary>
    /// <returns></returns>
    IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields();

    /// <summary>
    /// Export the object's data into a series of collections of rows,
    /// each collection being a different group of information.
    /// </summary>
    IEnumerable<(string StreamName, string[] RowValues)> Export();

    /// <summary>
    /// Returns true if the object is valid for export.
    /// For example, this could report false if an id is missing or invalid.
    /// </summary>
    public bool IsValid();

    /// <summary>
    /// Populates the current object from an XML reader.
    /// </summary>
    /// <param name="reader"></param>
    void Populate(XmlReader reader);
}
