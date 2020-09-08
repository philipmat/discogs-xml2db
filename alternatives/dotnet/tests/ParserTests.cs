using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using discogs;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace tests
{
    public class ParserTests
    {
        private static readonly Dictionary<string, Dictionary<string, int>> KnownCountsByFile = new Dictionary<string, Dictionary<string, int>>
        {
            ["labels"] = new Dictionary<string, int>
            {
                ["label"] = 1000,
                ["label_image"] = 421,
                ["label_url"] = 437
            },
            ["artists"] = new Dictionary<string, int>
            {
                ["artist_alias"] = 548,
                ["artist"] = 1000,
                ["artist_image"] = 530,
                ["artist_namevariation"] = 893,
                ["artist_url"] = 460,
                ["group_member"] = 218,
            },
            ["masters"] = new Dictionary<string, int>
            {
                ["master_artist"] = 1174,
                ["master_genre"] = 1315,
                ["master_image"] = 3020,
                ["master_style"] = 1705,
                ["master"] = 1000,
                ["master_video"] = 2176,
            },
            ["releases"] = new Dictionary<string, int>
            {
                ["release_artist"] = 5053,
                ["release_company"] = 1678,
                ["release_format"] = 1037,
                ["release_genre"] = 1302,
                ["release_identifier"] = 1672,
                ["release_image"] = 2897,
                ["release_label"] = 1135,
                ["release_style"] = 1496,
                ["release_track_artist"] = 6721,
                ["release_track"] = 9430,
                ["release_video"] = 2324,
                ["release"] = 1000,
            },
        };

        [Fact]
        public async Task ParseStream_Artist_CallsExporterForEveryTopNodeAsync()
            => await ParseStream_Type_CallsExporterForEveryTopNodeAsync<discogs.Artists.artist>("artists", 1_000);

        [Fact]
        public async Task ParseStream_Label_CallsExporterForEveryTopNodeAsync()
            => await ParseStream_Type_CallsExporterForEveryTopNodeAsync<discogs.Labels.label>("labels", 1_000);

        [Fact]
        public async Task ParseStream_Master_CallsExporterForEveryTopNodeAsync()
            => await ParseStream_Type_CallsExporterForEveryTopNodeAsync<discogs.Masters.master>("masters", 1_000);

        [Fact]
        public async Task ParseStream_Release_CallsExporterForEveryTopNodeAsync()
            => await ParseStream_Type_CallsExporterForEveryTopNodeAsync<discogs.Releases.release>("releases", 1_000);

        [Fact]
        public async Task ParseStream_Artist_KnownCounts() => await ParseStream_Type_TotalCounts<discogs.Artists.artist>("artists");

        [Fact]
        public async Task ParseStream_Label_KnownCounts() => await ParseStream_Type_TotalCounts<discogs.Labels.label>("labels");

        [Fact]
        public async Task ParseStream_Master_KnownCounts() => await ParseStream_Type_TotalCounts<discogs.Masters.master>("masters");

        [Fact]
        public async Task ParseStream_Release_KnownCounts() => await ParseStream_Type_TotalCounts<discogs.Releases.release>("releases");

        private static async Task ParseStream_Type_CallsExporterForEveryTopNodeAsync<T>(string file, int exportCallsCount)
            where T : IExportToCsv, new()
        {
            // Given
            int counter = 0;
            var exporter = Substitute.For<IExporter<T>>();
            exporter.WhenForAnyArgs(x => x.ExportAsync(Arg.Any<T>()))
                .Do(ci => counter++);

            using var stream = TestCommons.GetResourceStream($"discogs_20200806_{file}.xml.gz");
            using var gzStream = new GZipStream(stream, CompressionMode.Decompress);

            var p = new Parser<T>(exporter);

            // When
            await p.ParseStreamAsync(gzStream);

            // Then
            counter.Should().Be(exportCallsCount, because: $"there are {exportCallsCount:n0} records in the {file} xml file");
        }
        private static async Task ParseStream_Type_TotalCounts<T>(string file)
            where T : IExportToCsv, new()
        {
            // Given
            var knownFileCountsByScheme = KnownCountsByFile[file];
            Dictionary<string, int> countsByScheme = knownFileCountsByScheme.ToDictionary(kvp => kvp.Key, kvp => 0);
            void AddCounts(T obj)
            {
                foreach (var (scheme, _) in obj.ExportToCsv())
                {
                    countsByScheme[scheme] += 1;
                }
            }

            var exporter = Substitute.For<IExporter<T>>();
            await exporter.ExportAsync(Arg.Do<T>(AddCounts));

            using var stream = TestCommons.GetResourceStream($"discogs_20200806_{file}.xml.gz");
            using var gzStream = new GZipStream(stream, CompressionMode.Decompress);

            var p = new Parser<T>(exporter);

            // When
            await p.ParseStreamAsync(gzStream);

            // Then
            countsByScheme.Should().Equal(knownFileCountsByScheme);
        }
    }
}