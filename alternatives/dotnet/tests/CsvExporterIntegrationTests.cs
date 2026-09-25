namespace tests;

public class CsvExporterIntegrationTests : IDisposable
{
    private static readonly string _testPath = Path.Combine(Path.GetTempPath(), "CvsExporter");

    public CsvExporterIntegrationTests()
    {
        // deletes the folder as a shorcut to cleaning out and starting the test with blank slate
        if (Directory.Exists(_testPath)) Directory.Delete(_testPath, recursive: true);
        Directory.CreateDirectory(_testPath);
    }

    public void Dispose()
    {
        Directory.Delete(_testPath, recursive: true);
    }

    [Fact]
    public async Task Export_DoesNotCreateBomAsync()
    {
        //Given
        var exporter = new CsvExporter<SimpleRecord>(_testPath, compress: false);
        var record = new SimpleRecord();

        //When
        await exporter.ExportAsync(record);
        await exporter.CompleteExportAsync(1);

        //Then
        string outputFile = Path.Combine(_testPath, "test_1.csv");
        File.Exists(outputFile).Should().BeTrue();
        string content = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);

        content.Should().StartWith("foo,bar");
    }

    private class SimpleRecord : IExportable
    {
        public IEnumerable<(string StreamName, string[] RowValues)> Export()
        {
            yield return ("test_1", ["1.0", "1.1"]);
        }

        public IReadOnlyDictionary<string, string[]> GetExportStreamsAndFields()
            => new Dictionary<string, string[]>
            {
                ["test_1"] = ["foo", "bar"]
            };


        public bool IsValid() => throw new NotImplementedException();

        public void Populate(XmlReader reader) => throw new NotImplementedException();
    }
}
