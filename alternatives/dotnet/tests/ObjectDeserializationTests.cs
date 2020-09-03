using System.IO;
using System.Threading.Tasks;
using System.Xml;
using discogs;
using FluentAssertions;
using Xunit;

namespace tests
{
    public class ObjectDeserializationTests
    {
        [Fact]
        public async Task Artist_DeserializesAllPropertiesAsync()
        {
            var artist = await DeserializeAsync<discogs.Artists.artist>("artist.xml");

            // Assert
            artist.id.Should().Be("27");
            artist.name.Should().Be("Cari Lekebusch");
            artist.profile.Should().NotBeNullOrEmpty();
            artist.data_quality.Should().Be("Needs Vote");
            artist.urls.Should().HaveCount(3);
            artist.urls[0].Should().Be("https://lekebusch.bandcamp.com/");
            artist.namevariations.Should().HaveCount(2);
            artist.namevariations[0].Should().Be("C Lekebusch");
            artist.namevariations[1].Should().Be("Cari Lekebusch den rykande Bönsyrsan", because: "escaped antities are transformed");
            artist.members.Should().HaveCount(2);
            artist.members[0].id.Should().Be("6549", because: "6549 is the first member");
            artist.members[0].value.Should().Be("Richard Worth", because: "Richard Worth is the first member");
            artist.aliases.Should().HaveCount(2);
            artist.aliases[0].id.Should().Be("89");
            artist.aliases[1].value.Should().Be("Braincell");
            /* TODO: groups
            artist.groups.Should().HaveCount(2);
            artist.groups[0].id.Should().Be("2");
            artist.groups[1].value.Should().Be("Puente Latino");
            */
        }

        [Fact]
        public void Artist_Populate()
        {
            // Arrange
            var artist = new discogs.Artists.artist();

            // Act
            Populate(artist, "artist.xml");

            // assert
            artist.id.Should().Be("27");
            artist.name.Should().Be("Cari Lekebusch");
            artist.profile.Should().NotBeNullOrEmpty();
            artist.data_quality.Should().Be("Needs Vote");
            artist.urls.Should().HaveCount(3);
            artist.urls[0].Should().Be("https://lekebusch.bandcamp.com/");
            artist.namevariations.Should().HaveCount(2);
            artist.namevariations[0].Should().Be("C Lekebusch");
            artist.namevariations[1].Should().Be("Cari Lekebusch den rykande Bönsyrsan", because: "escaped antities are transformed");
            artist.members.Should().HaveCount(2);
            artist.members[0].id.Should().Be("6549", because: "6549 is the first member");
            artist.members[0].value.Should().Be("Richard Worth", because: "Richard Worth is the first member");
            artist.aliases.Should().HaveCount(2);
            artist.aliases[0].id.Should().Be("89");
            artist.aliases[1].value.Should().Be("Braincell");
            /*
            artist.groups.Should().HaveCount(2);
            artist.groups[0].id.Should().Be("2");
            artist.groups[1].value.Should().Be("Puente Latino");
            */
        }

        [Fact]
        public void Label_Populate()
        {
            var label = new discogs.Labels.label();

            // Act
            Populate(label, "label.xml");

            // Assert
            label.id.Should().Be("1");
            label.name.Should().Be("Planet E");
            label.profile.Should().StartWith("[a=Carl Craig]'s");
            label.profile.Should().EndWith("as publisher.");
            label.data_quality.Should().Be("Correct");
            label.urls.Should().HaveCount(5);
            /*
            label.sublabels.Should().HaveCount(2);
            label.sublabels[0].SubId.Should().Be("86537");
            label.sublabels[0].SubName.Should().Be("Antidote (4)");
            label.sublabels[1].SubId.Should().Be("488315");
            label.sublabels[1].SubName.Should().Be("TWPENTY");
            */
            label.parentLabel.id.Should().Be("4711");
            label.parentLabel.name.Should().Be("Goldhead Music");
        }

        private static void Populate<T>(T obj, string resourceName)
            where T : IExportable
        {
            /*
            var exporter = NSubstitute.Substitute.For<IExporter<discogs.Artists.artist>>();
            var parser = new Parser<discogs.Artists.artist>(exporter);
            parser.ParseStreamAsync2(artistRes);
            /*/
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                // TODO: perf IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                XmlResolver = null,
            };
            using (Stream artistRes = TestCommons.GetResourceStream(resourceName))
            {
                using (XmlReader reader = XmlReader.Create(artistRes, settings))
                {
                    reader.MoveToContent();  // on root - artist
                    // reader.Read(); // on text between <artist> and first node; the first thing in Populate is Read, which takes it to first node within artist
                    obj.Populate(reader);

                }
            }
        }
        
        private static async Task<T> DeserializeAsync<T>(string resourceFileName)
            where T : IExportable, new()
        {
            var xml = await TestCommons.GetResourceAsync(resourceFileName);
            return new ParserProxy<T>().DeserializeProxy(xml);
        }

        public class ParserProxy<T> : Parser<T>
                where T : IExportable, new()
        {
            public ParserProxy() : base (null) { }

            public T DeserializeProxy(string content)
                => Deserialize(content);
        }
    }
}
