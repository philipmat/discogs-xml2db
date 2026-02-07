namespace discogs;

/// <summary>
/// Represents an interface for objects that can be exported for use in external systems or formats.
/// </summary>
public interface IExportable
{
    IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields();

    IEnumerable<(string StreamName, string[] RowValues)> Export();

    /// <summary>
    /// Returns true if the object is valid for export.
    /// For example, this could report false if an id is missing or invalid.
    /// </summary>
    public bool IsValid();
    void Populate(XmlReader reader);
}
