using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using discogs;
using FluentAssertions;
using Xunit;

namespace tests
{
    public class ObjectDeserializationTests
    {
        [Fact(Skip = "Resource changed to include top node; need to re-work")]
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
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                // TODO: perf IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                XmlResolver = null,
            };
            var artist = new discogs.Artists.artist();
            var exporter = NSubstitute.Substitute.For<IExporter<discogs.Artists.artist>>();
            using (Stream artistRes = TestCommons.GetResourceStream("artist.xml"))
            {
                //*
                using (XmlReader reader = XmlReader.Create(artistRes, settings))
                {
                    reader.MoveToContent();  // on artists
                    reader.Read(); // on text between <artists> and <artist>
                    reader.Read(); // on artist, but the first thing in Populate is Read, which takes it to first node within artist
                    // Act
                    artist.Populate(reader);

                }
                //*/
                /*
                var parser = new Parser<discogs.Artists.artist>(exporter);
                parser.ParseStreamAsync2(artistRes);
                /*/

            }

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
            artist.groups.Should().HaveCount(2);
            artist.groups[0].id.Should().Be("2");
            artist.groups[1].value.Should().Be("Puente Latino");
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
