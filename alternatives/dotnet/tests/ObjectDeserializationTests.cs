using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
        
        private static async Task<T> DeserializeAsync<T>(string resourceFileName)
            where T : IExportToCsv, new()
        {
            var xml = await TestCommons.GetResourceAsync(resourceFileName);
            return new ParserProxy<T>().DeserializeProxy(xml);
        }

        public class ParserProxy<T> : Parser<T>
                where T : IExportToCsv, new()
        {
            public ParserProxy() : base (null) { }

            public T DeserializeProxy(string content)
                => Deserialize(content);
        }
    }
}
