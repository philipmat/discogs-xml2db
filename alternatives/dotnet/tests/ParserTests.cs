using System.IO.Compression;
using System.Threading.Tasks;
using discogs;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace tests
{
    public class ParserTests
    {
        [Fact]
        public async Task ParseStream_Artist_CallsExporterForEveryTopNodeAsync()
        {
            // Given
            int counter = 0;
            var exporter = Substitute.For<IExporter<discogs.Artists.artist>>();
            exporter.WhenForAnyArgs(x => x.ExportAsync(null))
                .Do(ci => counter++);
            
            using var stream = TestCommons.GetResourceStream("discogs_20200806_artists.xml.gz");
            using var gzStream = new GZipStream(stream, CompressionMode.Decompress);
            
            var p = new Parser<discogs.Artists.artist>(exporter);

            // When
            await p.ParseStreamAsync(gzStream);
            
            // Then
            counter.Should().Be(1_000, because: "there are 1,000 records in the xml file");
        }
    }
}