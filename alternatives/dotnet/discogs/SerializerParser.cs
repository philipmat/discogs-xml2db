namespace discogs;

public class SerializerParser<T>(IExporter<T> exporter, int throttle = 1) : Parser<T>(exporter, throttle)
    where T : IExportable, new()
{
    private readonly XmlSerializer _serializer = new(typeof(T));

    protected override async Task<T> ReadObject(XmlReader positionedReader)
    {
        string objectString = await positionedReader.ReadOuterXmlAsync();
        if (string.IsNullOrEmpty(objectString))
        {
            return default(T);
        }

        try
        {
            return Deserialize(objectString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error {ex} parsing node {objectString}");
            return default(T);
        }
    }

    protected T Deserialize(string objectString)
    {
        using var reader = new StringReader(objectString);
        var obj = (T)_serializer.Deserialize(reader);
        return obj;
    }
}
