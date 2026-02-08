namespace tests;

public class ObjectDeserializationTests
{
    [Fact]
    public async Task Artist_DeserializesAllProperties_WithXmlSerializer()
    {
        Artist artist = await DeserializeAsync<Artist>("artist.xml");

        // Assert
        artist.Id.Should().Be("27");
        artist.Name.Should().Be("Cari Lekebusch");
        artist.RealName.Should().Be("Kari Pekka Lekebusch");
        artist.Profile.Should().Match("Capricorn born *");
        artist.DataQuality.Should().Be("Needs Vote");
        artist.Urls.Should()
            .HaveCount(3)
            .And
            .AllSatisfy(u => u.Should().Contain("lekebusch"));
        artist.Urls[0].Should().Be("https://lekebusch.bandcamp.com/");
        artist.NameVariations.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                n => n.Should().Be("C Lekebusch"),
                n => n.Should()
                    .Be("Cari Lekebusch den rykande Bönsyrsan", because: "escaped entities are transformed"));
        artist.Members.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                m =>
                {
                    m.Id.Should().Be("6549", because: "6549 is the first member");
                    m.Value.Should().Be("Richard Worth", because: "Richard Worth is the first member");
                },
                m =>
                {
                    m.Id.Should().Be("28896", because: "28896 is the second member");
                    m.Value.Should().Be("Jay Rodriguez");
                }
            );
        artist.Aliases.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                a => a.Id.Should().Be("89"),
                a => a.Value.Should().Be("Braincell")
            );
        artist.Groups.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                g => g.Id.Should().Be("2"),
                g => g.Value.Should().Be("Puente Latino")
            );
    }

    [Fact]
    public void Artist_Populate()
    {
        // Arrange
        Artist artist = new();

        // Act
        Populate(artist, "artist.xml");

        // assert
        artist.Id.Should().Be("27");
        artist.Name.Should().Be("Cari Lekebusch");
        artist.RealName.Should().Be("Kari Pekka Lekebusch");
        artist.Profile.Should().Match("Capricorn born *");
        artist.DataQuality.Should().Be("Needs Vote");
        artist.Urls.Should()
            .HaveCount(3)
            .And
            .AllSatisfy(u => u.Should().Contain("lekebusch"));
        artist.Urls[0].Should().Be("https://lekebusch.bandcamp.com/");
        artist.NameVariations.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                n => n.Should().Be("C Lekebusch"),
                n => n.Should()
                    .Be("Cari Lekebusch den rykande Bönsyrsan", because: "escaped entities are transformed"));
        artist.Members.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                m =>
                {
                    m.Id.Should().Be("6549", because: "6549 is the first member");
                    m.Value.Should().Be("Richard Worth", because: "Richard Worth is the first member");
                },
                m =>
                {
                    m.Id.Should().Be("28896", because: "28896 is the second member");
                    m.Value.Should().Be("Jay Rodriguez");
                }
            );
        artist.Aliases.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                a => a.Id.Should().Be("89"),
                a => a.Value.Should().Be("Braincell")
            );
        artist.Groups.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                g => g.Id.Should().Be("2"),
                g => g.Value.Should().Be("Puente Latino")
            );

        artist.Images.Should()
            .HaveCount(5)
            .And
            .AllSatisfy(i =>
            {
                i.Type.Should().BeOneOf("primary", "secondary");
                i.Uri.Should().BeNullOrEmpty();
                i.Uri150.Should().BeNullOrEmpty();
                i.Width.Should().HaveLength(3);
                i.Height.Should().HaveLength(3);
            });
    }

    [Fact]
    public void Label_Populate()
    {
        var label = new Label();

        // Act
        Populate(label, "label.xml");

        // Assert
        label.Id.Should().Be("1");
        label.Name.Should().Be("Planet E");
        label.Profile.Should().StartWith("[a=Carl Craig]'s");
        label.Profile.Should().EndWith("as publisher.");
        label.DataQuality.Should().Be("Correct");
        label.Urls.Should()
            .HaveCount(5)
            .And
            .AllSatisfy(u => u.Should().StartWith("http"));
        label.ParentLabel.Id.Should().Be("4711");
        label.ParentLabel.Name.Should().Be("Goldhead Music");
        label.Sublabels.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                l =>
                {
                    l.Id.Should().Be("86537");
                    l.Name.Should().Be("Antidote (4)");
                },
                l =>
                {
                    l.Id.Should().Be("488315");
                    l.Name.Should().Be("TWPENTY");
                }
            )
            .And
            .AllSatisfy(l => l.IsSubLabel.Should().BeTrue());
        label.Images.Should()
            .HaveCount(3)
            .And
            .AllSatisfy(i =>
            {
                i.Type.Should().BeOneOf("primary", "secondary");
                i.Uri.Should().BeNullOrEmpty();
                i.Uri150.Should().BeNullOrEmpty();
                i.Width.Should().MatchRegex("\\d+");
                i.Height.Should().MatchRegex("\\d+");
            });
    }

    [Fact]
    public void Master_Populate()
    {
        Master master = new();

        // Act
        Populate(master, "master.xml");

        // Assert
        master.Id.Should().Be("122");
        master.MainRelease.Should().Be("85912");
        master.Year.Should().Be("1993");
        master.Title.Should().Be("Organized Chaos E.P.");
        master.DataQuality.Should().Be("Correct");
        master.Images.Should()
            .HaveCount(2)
            .And
            .AllSatisfy(i =>
            {
                i.Type.Should().BeOneOf("primary", "secondary");
                i.Uri.Should().BeNullOrEmpty();
                i.Uri150.Should().BeNullOrEmpty();
                i.Width.Should().HaveLength(3);
                i.Height.Should().HaveLength(3);
            });

        master.Artists.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                a =>
                {
                    a.Id.Should().Be("69209");
                    a.Name.Should().Be("Mixrace");
                    a.ArtistNameVariation.Should().BeNullOrEmpty();
                    a.Join.Should().BeNullOrEmpty();
                    a.Role.Should().BeNullOrEmpty();
                    a.Tracks.Should().BeNullOrEmpty();
                },
                a =>
                {
                    a.Id.Should().Be("123");
                    a.Name.Should().Be("Second Artist");
                    a.ArtistNameVariation.Should().Be("Artist Name Variation");
                    a.Join.Should().BeNullOrEmpty();
                    a.Role.Should().BeNullOrEmpty();
                    a.Tracks.Should().BeNullOrEmpty();
                }
            );

        master.Genres.Should()
            .HaveCount(1)
            .And.BeEquivalentTo("Electronic");
        master.Styles.Should()
            .HaveCount(3)
            .And.BeEquivalentTo("Breakbeat", "Hardcore", "Jungle");

        master.Videos.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                v =>
                {
                    v.Src.Should().Be("https://www.youtube.com/watch?v=mksCnb_USuc");
                    v.Duration.Should().Be("364");
                    v.Embed.Should().Be("true");
                    v.Title.Should().Be("Mix Race - Mixrace Outta Hand");
                    v.Description.Should().Be("[SHADOW 28] Mixrace - Organized Chaos EP (1992)");
                },
                v =>
                {
                    v.Src.Should().Be("https://www.youtube.com/watch?v=UNxtcvAoP_0");
                    v.Duration.Should().Be("320");
                    v.Embed.Should().Be("true");
                    v.Title.Should().Be("Mix Race - Dance with the Devil");
                    v.Description.Should().Be("[SHADOW 28] Mixrace - Organized Chaos EP (1992)");
                }
            );
    }

    [Fact]
    public void Release_Populate()
    {
        Release release = new();

        // Act
        Populate(release, "release.xml");

        // Assert
        release.Title.Should().Be("Profound Sounds Vol. 1");
        release.Country.Should().Be("US");
        release.Released.Should().Be("1999-07-13");
        release.Notes.Should().NotBeNullOrEmpty();
        release.DataQuality.Should().Be("Correct");
        release.MasterId.Should().Be("66526");

        release.Images.Should()
            .HaveCount(4)
            .And
            .AllSatisfy(i =>
            {
                i.Type.Should().BeOneOf("primary", "secondary");
                i.Uri.Should().BeNullOrEmpty();
                i.Uri150.Should().BeNullOrEmpty();
                i.Width.Should().HaveLength(3);
                i.Height.Should().HaveLength(3);
            });

        release.Videos.Should()
            .HaveCount(3)
            .And
            .SatisfyRespectively(
                r =>
                {
                    r.Src.Should().Be("https://www.youtube.com/watch?v=bqUfNGJEKlo");
                    r.Duration.Should().Be("4074");
                    r.Embed.Should().Be("true");
                    r.Title.Should().Be("Profound Sounds Vol. 1 - Josh Wink");
                    r.Description.Should().Be("mix 1999");
                },
                r =>
                {
                    r.Src.Should().NotBeNullOrWhiteSpace();
                    r.Duration.Should().NotBeNullOrWhiteSpace();
                    r.Embed.Should().Be("true");
                    r.Title.Should().NotBeNullOrWhiteSpace();
                    r.Description.Should().BeNullOrEmpty();
                },
                r =>
                {
                    r.Src.Should().Be("https://www.youtube.com/watch?v=cpQWEQjunF4");
                    r.Duration.Should().Be("421");
                    r.Embed.Should().Be("true");
                    r.Title.Should().Be("Profound Sounds Track 1....");
                    r.Description.Should().Be("How it SHOULD sound......");
                }
            );

        release.Genres.Should()
            .HaveCount(1)
            .And.BeEquivalentTo("Electronic");
        release.Styles.Should()
            .HaveCount(2)
            .And.BeEquivalentTo("Tech House", "Techno");

        release.Identifiers.Should()
            .HaveCount(2)
            .And.SatisfyRespectively(
                i =>
                {
                    i.Type.Should().Be("Barcode");
                    i.Value.Should().Be("074646362822");
                    i.Description.Should().BeNullOrEmpty();
                },
                i =>
                {
                    i.Type.Should().Be("Matrix / Runout");
                    i.Value.Should().Be("G PHRUPMASTERGENERAL T27 LONDON");
                    i.Description.Should().Be("Only On A-Side Runout");
                }
            );

        release.Labels.Should()
            .HaveCount(1)
            .And
            .SatisfyRespectively(l =>
            {
                l.Id.Should().Be("6");
                l.Name.Should().Be("Ruffhouse Records");
                l.CatalogNumber.Should().Be("CK 63628");
            });

        release.Formats.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                f =>
                {
                    f.Name.Should().Be("Cassette");
                    f.Quantity.Should().Be("15");
                    f.Text.Should().BeNullOrEmpty();
                    f.Descriptions.Should().BeNullOrEmpty();
                },
                f =>
                {
                    f.Name.Should().Be("CD");
                    f.Quantity.Should().Be("1");
                    f.Text.Should().Be("cd text");
                    f.Descriptions.Should().BeEquivalentTo(["Compilation", "Mixed"]);
                });

        release.Artists.Should()
            .HaveCount(1)
            .And
            .SatisfyRespectively(a =>
            {
                a.Id.Should().Be("3");
                a.Name.Should().Be("Josh Wink");
                a.ArtistNameVariation.Should().BeNullOrEmpty();
                a.Join.Should().BeNullOrEmpty();
                a.Role.Should().BeNullOrEmpty();
                a.Tracks.Should().BeNullOrEmpty();
            });

        release.ExtraArtists.Should()
            .HaveCount(1)
            .And.SatisfyRespectively(e =>
            {
                e.Id.Should().Be("3");
                e.Name.Should().Be("Josh Wink");
                e.ArtistNameVariation.Should().BeNullOrEmpty();
                e.Join.Should().BeNullOrEmpty();
                e.Role.Should().Be("DJ Mix");
                e.Tracks.Should().BeNullOrEmpty();
            });

        release.Companies.Should()
            .HaveCount(2)
            .And
            .SatisfyRespectively(
                c =>
                {
                    c.Id.Should().Be("93330");
                    c.Name.Should().Be("Columbia Records");
                    c.CatalogNumber.Should().Be("1");
                    c.EntityType.Should().Be("10");
                    c.EntityTypeName.Should().Be("Manufactured By");
                    c.ResourceUrl.Should().Be("https://api.discogs.com/labels/93330");
                },
                c =>
                {
                    c.Id.Should().Be("93330");
                    c.Name.Should().Be("Columbia Records");
                    c.CatalogNumber.Should().BeNullOrEmpty();
                    c.EntityType.Should().Be("9");
                    c.EntityTypeName.Should().Be("Distributed By");
                    c.ResourceUrl.Should().Be("https://api.discogs.com/labels/93330");
                });

        release.TrackList.Should()
            .HaveCount(3)
            .And.SatisfyRespectively(
                t =>
                {
                    t.Position.Should().Be("1");
                    t.Title.Should().Be("Untitled 8");
                    t.Duration.Should().Be("7:00");

                    t.Artists.Should().HaveCount(2);
                    t.Artists[0].Id.Should().Be("5");
                    t.Artists[0].Name.Should().Be("Heiko Laux");
                    t.Artists[0].Join.Should().Be("&");
                    t.Artists[1].Id.Should().Be("4");
                    t.Artists[1].Name.Should().Be("Johannes Heil");
                    t.Artists[1].Join.Should().BeNullOrEmpty();

                    t.ExtraArtists.Should().ContainSingle();
                    t.ExtraArtists[0].Id.Should().Be("11233");
                    t.ExtraArtists[0].Name.Should().Be("Chris Lum");
                    t.ExtraArtists[0].Role.Should().Be("Producer");

                    t.SubTracks.Should().HaveCount(3);
                    t.SubTracks[0].Position.Should().Be("11.a");
                    t.SubTracks[0].Title.Should().Be("909 Shuffle");
                    t.SubTracks[0].Duration.Should().Be("3:10");
                    t.SubTracks[1].Position.Should().Be("11.b");
                    t.SubTracks[1].Title.Should().Be("Laser 101 Rmx");
                    t.SubTracks[2].Position.Should().Be("11.c");
                    t.SubTracks[2].Duration.Should().Be("5:38");
                },
                t =>
                {
                    t.Position.Should().Be("2");
                    t.Title.Should().Be("Anjua (Sneaky 3)");
                    t.Duration.Should().Be("5:28");

                    t.SubTracks.Should().BeNullOrEmpty();
                    t.Artists.Should().ContainSingle();
                    t.ExtraArtists.Should().BeNullOrEmpty();
                    t.SubTracks.Should().BeNullOrEmpty();
                    t.Artists.Should().ContainSingle();
                    t.ExtraArtists.Should().BeEmpty();
                },
                t =>
                {
                    t.Position.Should().Be("3");
                    t.Title.Should().Be("When The Funk Hits The Fan (Mood II Swing When The Dub Hits The Fan)");
                    t.Duration.Should().Be("5:25");
                    t.Artists.Should().ContainSingle();
                    t.ExtraArtists.Should().ContainSingle();
                    t.SubTracks.Should().BeEmpty();
                });
    }

    [Fact]
    public void Release_4497890_Populate_TrackArtists()
    {
        var release = new Release();

        // Act
        Populate(release, "release_4497890.xml");

        var t1 = release.TrackList[0];
        t1.Position.Should().Be("1-1");
        t1.Artists.Should().BeNullOrEmpty();
        t1.ExtraArtists.Should().HaveCount(1);
        t1.ExtraArtists[0].Name.Should().Be("Emerson, Lake & Palmer");

        var t2 = release.TrackList[1];
        t2.Position.Should().BeNullOrEmpty();
        t2.Title.Should().Be("Piano Concerto No. 1");
        t2.Artists.Should().BeNullOrEmpty();
        t2.ExtraArtists.Should().HaveCount(2);
        t2.SubTracks.Should().HaveCount(1);
        t2.SubTracks[0].Position.Should().Be("1-2");


        var t3 = release.TrackList[2];
        t3.Position.Should().Be("1-3");
        t3.Artists.Should().BeNullOrEmpty();
        t3.ExtraArtists.Should().HaveCount(1);
        t3.ExtraArtists[0].Name.Should().Be("Greg Lake");

        var t4 = release.TrackList[3];
        t4.Position.Should().BeNullOrEmpty();
        t4.Title.Should().Be("Karn Evil 9");
        t4.Artists.Should().BeNullOrEmpty();
        t4.ExtraArtists.Should().BeNullOrEmpty();
        t4.SubTracks.Should().HaveCount(1);
        t4.SubTracks[0].Position.Should().Be("1-4");
        t4.SubTracks[0].Artists.Should().BeNullOrEmpty();
        t4.SubTracks[0].ExtraArtists.Should().HaveCount(2);
        t4.SubTracks[0].ExtraArtists[0].Name.Should().Be("Greg Lake");
        t4.SubTracks[0].ExtraArtists[1].Name.Should().Be("Keith Emerson");
    }


    [DebugOnly]
    public async Task Artist_11037_DeserializationMatchesBothApproaches()
    {
        var artist = await DeserializeAsync<Artist>("artist_11037.xml");

        // Assert
        artist.Id.Should().Be("11037");
        artist.Name.Should().Be("Soul Boy");
        artist.RealName.Should().Be("M. Marsico, L. Macchiaizzano\rif  M. Marsico & L. M");

        var artists = RetrieveObjects<Artist>("artist_problems.xml")
            .ToList();

        artists.Should().HaveCount(1);
        artists[0].Id.Should().Be(artist.Id);
        artists[0].Name.Should().Be(artist.Name);
        artists[0].RealName.Should().Be(artist.RealName);
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
        using Stream artistRes = TestCommons.GetResourceStream(resourceName);
        using XmlReader reader = XmlReader.Create(artistRes, Parser<T>.DefaultReaderSettings);
        reader.MoveToContent(); // on root - artist
        // reader.Read(); // on text between <artist> and the first node; the first thing in Populate is Read, which takes it to the first node within artist
        obj.Populate(reader);

        //*/
    }

    private static IEnumerable<T> RetrieveObjects<T>(string resourceName)
        where T : IExportable, new()
    {
        var objs = new List<T>();
        var exporter = Substitute.For<IExporter<T>>();
        exporter.WhenForAnyArgs(x => x.ExportAsync(Arg.Any<T>()))
            .Do(ci => objs.Add(ci.Arg<T>()));
        using Stream resStream = TestCommons.GetResourceStream(resourceName);
        var parser = new Parser<T>(exporter);
        parser.ParseStreamAsync(resStream).Wait();

        return objs;
    }

    private static async Task<T> DeserializeAsync<T>(string resourceFileName)
        where T : IExportable, new()
    {
        string xml = await TestCommons.GetResourceAsync(resourceFileName);
        return new ParserProxy<T>().DeserializeProxy(xml);
    }


    private class ParserProxy<T>() : XmlSerializerBasedParser<T>(null)
        where T : IExportable, new()
    {
        public T DeserializeProxy(string content)
            => Deserialize(content);
    }
}
