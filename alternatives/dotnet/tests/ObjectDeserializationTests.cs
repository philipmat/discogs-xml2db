using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using discogs;
using FluentAssertions;
using NSubstitute;
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
            artist.images.Should().HaveCount(5);
            artist.images[0].type.Should().Be("primary");
            artist.images[0].uri.Should().BeNullOrEmpty();
            artist.images[0].uri150.Should().BeNullOrEmpty();
            artist.images[0].width.Should().Be("333");
            artist.images[0].height.Should().Be("500");
            artist.images[1].type.Should().Be("secondary");
            artist.images[^1].type.Should().Be("secondary");
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
            label.images.Should().HaveCount(3);
            label.images[0].type.Should().Be("primary");
            label.images[0].uri.Should().BeNullOrEmpty();
            label.images[0].uri150.Should().BeNullOrEmpty();
            label.images[0].width.Should().Be("132");
            label.images[0].height.Should().Be("24");
            label.images[1].type.Should().Be("secondary");
            label.images[^1].type.Should().Be("secondary");
        }

        [Fact]
        public void Master_Populate()
        {
            var master = new discogs.Masters.master();

            // Act
            Populate(master, "master.xml");

            // Assert
            master.id.Should().Be("122");
            master.main_release.Should().Be("85912");
            master.year.Should().Be("1993");
            master.title.Should().Be("Organized Chaos E.P.");
            master.data_quality.Should().Be("Correct");
            master.images.Should().HaveCount(2);
            master.images[0].type.Should().Be("primary");
            master.images[0].uri.Should().BeNullOrEmpty();
            master.images[0].uri150.Should().BeNullOrEmpty();
            master.images[0].width.Should().Be("600");
            master.images[0].height.Should().Be("604");
            master.images[1].type.Should().Be("secondary");
            master.images[1].uri.Should().BeNullOrEmpty();
            master.images[1].uri150.Should().BeNullOrEmpty();
            master.images[1].width.Should().Be("600");
            master.images[1].height.Should().Be("604");

            master.artists.Should().HaveCount(2);
            master.artists[0].id.Should().Be("69209");
            master.artists[0].name.Should().Be("Mixrace");
            master.artists[0].anv.Should().BeNullOrEmpty();
            master.artists[0].join.Should().BeNullOrEmpty();
            master.artists[0].role.Should().BeNullOrEmpty();
            master.artists[0].tracks.Should().BeNullOrEmpty();
            master.artists[1].id.Should().Be("123");
            master.artists[1].name.Should().Be("Second Artist");
            master.artists[1].anv.Should().Be("Artist Name Variation");
            master.artists[1].join.Should().BeNullOrEmpty();
            master.artists[1].role.Should().BeNullOrEmpty();
            master.artists[1].tracks.Should().BeNullOrEmpty();

            master.genres.Should().HaveCount(1);
            master.genres[0].Should().Be("Electronic");
            master.styles.Should().HaveCount(3);
            master.styles[0].Should().Be("Breakbeat");
            master.styles[1].Should().Be("Hardcore");
            master.styles[2].Should().Be("Jungle");

            master.videos.Should().HaveCount(2);
            master.videos[0].src.Should().Be("https://www.youtube.com/watch?v=mksCnb_USuc");
            master.videos[0].duration.Should().Be("364");
            master.videos[0].embed.Should().Be("true");
            master.videos[0].title.Should().Be("Mix Race - Mixrace Outta Hand");
            master.videos[0].description.Should().Be("[SHADOW 28] Mixrace - Organized Chaos EP (1992)");
        }

        [Fact]
        public void Release_Populate()
        {
            var release = new discogs.Releases.release();

            // Act
            Populate(release, "release.xml");

            // Assert
            release.title.Should().Be("Profound Sounds Vol. 1");
            release.country.Should().Be("US");
            release.released.Should().Be("1999-07-13");
            release.notes.Should().NotBeNullOrEmpty();
            release.data_quality.Should().Be("Correct");
            release.master_id.Should().Be("66526");

            release.videos.Should().HaveCount(3);
            release.videos[0].src.Should().Be("https://www.youtube.com/watch?v=bqUfNGJEKlo");
            release.videos[0].duration.Should().Be("4074");
            release.videos[0].embed.Should().Be("true");
            release.videos[0].title.Should().Be("Profound Sounds Vol. 1 - Josh Wink");
            release.videos[0].description.Should().Be("mix 1999");
            release.videos[^1].src.Should().Be("https://www.youtube.com/watch?v=cpQWEQjunF4");
            release.videos[^1].duration.Should().Be("421");
            release.videos[^1].embed.Should().Be("true");
            release.videos[^1].title.Should().Be("Profound Sounds Track 1....");
            release.videos[^1].description.Should().Be("How it SHOULD sound......");

            release.genres.Should().HaveCount(1);
            release.genres[0].Should().Be("Electronic");
            release.styles.Should().HaveCount(2);
            release.styles[0].Should().Be("Techno");
            release.styles[1].Should().Be("Tech House");
        }

        private static void Populate<T>(T obj, string resourceName)
            where T : IExportable, new()
        {
            /*
            using Stream resStream = TestCommons.GetResourceStream(resourceName);
            var exporter = NSubstitute.Substitute.For<IExporter<T>>();
            var parser = new Parser<T>(exporter);
            parser.ParseStreamAsync2(resStream).Wait();
            return;
            //*/
            //*
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                // TODO: perf IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
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
            //*/
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
