using System.Text;

namespace tests;

/// <summary>
/// Self-closing &lt;video .../&gt; elements (no title/description children) must not make the
/// parser read past the end of the owning master/release.
/// </summary>
public class VideoParsingTests
{
    private const string EmptyVideo = """<video src="https://www.youtube.com/watch?v=empty" duration="0" embed="true"/>""";

    private static string FullVideo(string id) =>
        $"""<video src="https://www.youtube.com/watch?v={id}" duration="120" embed="true"><title>Video {id}</title><description>Desc {id}</description></video>""";

    private static async Task<List<T>> ParseAsync<T>(string xml)
        where T : IExportable, new()
    {
        List<T> parsed = [];
        var exporter = Substitute.For<IExporter<T>>();
        await exporter.ExportAsync(Arg.Do<T>(parsed.Add));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await new Parser<T>(exporter).ParseStreamAsync(stream);
        return parsed;
    }

    private static string Master(string id, string title, params string[] videos) =>
        $"""<master id="{id}"><main_release>{id}0</main_release><title>{title}</title><data_quality>Correct</data_quality><videos>{string.Concat(videos)}</videos></master>""";

    private static string Release(string id, string title, params string[] videos) =>
        $"""<release id="{id}" status="Accepted"><title>{title}</title><country>UK</country><data_quality>Correct</data_quality><videos>{string.Concat(videos)}</videos></release>""";

    [Fact]
    public async Task Master_LastVideoSelfClosing_DoesNotSwallowNextMaster()
    {
        string xml = "<masters>"
                     + Master("1", "First", EmptyVideo)
                     + Master("2", "Second", FullVideo("a"), FullVideo("b"))
                     + "</masters>";

        List<Master> masters = await ParseAsync<Master>(xml);

        masters.Select(m => (m.Id, m.Title)).Should().Equal(("1", "First"), ("2", "Second"));
        masters[0].Videos.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Src = "https://www.youtube.com/watch?v=empty", Title = (string)null });
        masters[1].Videos.Select(v => v.Title).Should().Equal("Video a", "Video b");
    }

    [Fact]
    public async Task Master_SelfClosingVideoFollowedBySibling_KeepsTitleAndAllVideos()
    {
        string xml = "<masters>"
                     + Master("1", "First", FullVideo("a"), EmptyVideo, FullVideo("b"))
                     + Master("2", "Second", FullVideo("c"))
                     + "</masters>";

        List<Master> masters = await ParseAsync<Master>(xml);

        masters.Select(m => (m.Id, m.Title)).Should().Equal(("1", "First"), ("2", "Second"));
        masters[0].Videos.Select(v => v.Title).Should().Equal("Video a", null, "Video b");
        masters[0].Videos.Select(v => v.Description).Should().Equal("Desc a", null, "Desc b");
        masters[1].Videos.Select(v => v.Title).Should().Equal("Video c");
    }

    [Fact]
    public async Task Master_EmptyVideosElement_ParsesNextMaster()
    {
        string xml = "<masters>"
                     + """<master id="1"><title>First</title><videos/><data_quality>Correct</data_quality></master>"""
                     + Master("2", "Second", FullVideo("a"))
                     + "</masters>";

        List<Master> masters = await ParseAsync<Master>(xml);

        masters.Select(m => (m.Id, m.Title, m.DataQuality)).Should().Equal(("1", "First", "Correct"), ("2", "Second", "Correct"));
    }

    [Fact]
    public async Task Release_LastVideoSelfClosing_DoesNotSwallowNextRelease()
    {
        string xml = "<releases>"
                     + Release("1", "First", FullVideo("a"), EmptyVideo)
                     + Release("2", "Second", FullVideo("b"))
                     + "</releases>";

        List<Release> releases = await ParseAsync<Release>(xml);

        releases.Select(r => (r.Id, r.Title)).Should().Equal(("1", "First"), ("2", "Second"));
        releases[0].Videos.Select(v => v.Title).Should().Equal("Video a", null);
        releases[1].Videos.Select(v => v.Title).Should().Equal("Video b");
    }

    [Fact]
    public async Task Release_SelfClosingVideoFollowedBySibling_KeepsTitleAndAllVideos()
    {
        string xml = "<releases>"
                     + Release("1", "First", EmptyVideo, FullVideo("a"))
                     + Release("2", "Second", FullVideo("b"))
                     + "</releases>";

        List<Release> releases = await ParseAsync<Release>(xml);

        releases.Select(r => (r.Id, r.Title)).Should().Equal(("1", "First"), ("2", "Second"));
        releases[0].Videos.Select(v => v.Title).Should().Equal(null, "Video a");
    }
}
