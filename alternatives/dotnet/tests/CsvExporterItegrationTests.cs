using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using discogs;
using FluentAssertions;
using Xunit;

namespace tests
{
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
            byte[] content = await File.ReadAllBytesAsync(outputFile);

            content[0].Should().Be((byte)'f', because: "foo is the first word in the file");
            content[1].Should().Be((byte)'o', because: "foo is the first word in the file");
        }

        private class SimpleRecord : IExportToCsv
        {
            public IEnumerable<(string StreamName, string[] RowValues)> ExportToCsv()
            {
                yield return ("test_1", new string[] { "1.0", "1.1" });
            }

            public IReadOnlyDictionary<string, string[]> GetCsvExportScheme()
                => new Dictionary<string, string[]> {
                    ["test_1"] = new string[] { "foo", "bar" }
                };
        }
    }
}