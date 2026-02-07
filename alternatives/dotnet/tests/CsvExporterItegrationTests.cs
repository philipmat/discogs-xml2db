namespace tests;

public class CsvExporterIntegrationTests : IDisposable
{
    public static readonly string TestPath = Path.Combine(Path.GetTempPath(), "CvsExporter");

    public CsvExporterIntegrationTests()
    {
        // deletes folder as a shorcut to cleaning out and starting the test with blank slate
        if (Directory.Exists(TestPath)) Directory.Delete(TestPath, recursive: true);
        Directory.CreateDirectory(TestPath);
    }

    public void Dispose()
    {
        Directory.Delete(TestPath, recursive: true);
    }

    [Fact]
    public async Task Export_DoesNotCreateBomAsync()
    {
        //Given
        var exporter = new CsvExporter<SimpleRecord>(TestPath, compress: false);
        var record = new SimpleRecord();

        //When
        await exporter.ExportAsync(record);
        await exporter.CompleteExportAsync(1);

        //Then
        var outputFile = Path.Combine(TestPath, "test_1.csv");
        File.Exists(outputFile).Should().BeTrue();
        string content = await File.ReadAllTextAsync(outputFile);

        content.Should().StartWith("foo,bar");
    }

    private class SimpleRecord : IExportable
    {
        public IEnumerable<(string StreamName, string[] RowValues)> Export()
        {
            yield return ("test_1", new string[] { "1.0", "1.1" });
        }

        public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
            => new Dictionary<string, string[]> {
                ["test_1"] = new string[] { "foo", "bar" }
            };


        public bool IsValid() => throw new NotImplementedException();

        public void Populate(XmlReader reader) => throw new NotImplementedException();
    }
}
