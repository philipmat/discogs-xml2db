using System.Text;
using System.Text.RegularExpressions;

namespace tests;

/// <summary>
/// A self-closing element (<c>&lt;x/&gt;</c>) must parse exactly like its explicit form (<c>&lt;x&gt;&lt;/x&gt;</c>)
/// and must not affect the parsing of neighbouring records.
/// Each case parses three records A, B, C where B contains the self-closing element (first or last in B),
/// and expects the same exported rows as parsing A, B (with the element expanded) and C on their own.
/// </summary>
public partial class SelfClosingElementTests
{
    [GeneratedRegex(@"<([\w:]+)((?:\s[^<>]*?)?)\s*/>")]
    private static partial Regex SelfClosingTag();

    private static string Expand(string xml) => SelfClosingTag().Replace(xml, "<$1$2></$1>");

    private static async Task<List<string>> ParseToRowsAsync<T>(string root, params string[] records)
        where T : IExportable, new()
    {
        List<string> parsed = [];
        var exporter = Substitute.For<IExporter<T>>();
        await exporter.ExportAsync(Arg.Do<T>(obj => parsed.Add(string.Join("\n",
            obj.Export().Select(r => $"{r.StreamName}: {string.Join(" | ", r.RowValues.Select(v => v ?? ""))}")))));

        string xml = $"<{root}>{string.Concat(records)}</{root}>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        await new Parser<T>(exporter).ParseStreamAsync(stream);
        return parsed;
    }

    private static async Task AssertParsedIndependentlyAsync<T>(string root, string a, string b, string c)
        where T : IExportable, new()
    {
        b.Should().MatchRegex(SelfClosingTag().ToString(), because: "the case must contain a self-closing element");

        List<string> expected =
        [
            .. await ParseToRowsAsync<T>(root, a),
            .. await ParseToRowsAsync<T>(root, Expand(b)),
            .. await ParseToRowsAsync<T>(root, c),
        ];

        List<string> actual = await ParseToRowsAsync<T>(root, a, b, c);

        actual.Should().Equal(expected);
    }

    private static TheoryData<string, bool> Cases(params string[] snippets)
    {
        TheoryData<string, bool> data = [];
        foreach (string s in snippets)
        {
            data.Add(s, false);
            data.Add(s, true);
        }

        return data;
    }

    private static string Place(string fields, string snippet, bool atEnd) => atEnd ? fields + snippet : snippet + fields;

    #region artists

    private const string ArtistA = """
        <artist><images><image type="primary" uri="" uri150="" width="1" height="1"/></images><id>1</id><name>Artist A</name>
        <realname>Real A</realname><profile>Profile A</profile><data_quality>Correct</data_quality>
        <urls><url>http://a.example</url></urls><namevariations><name>A Var</name></namevariations>
        <aliases><name id="11">Alias A</name></aliases><members><id>12</id><name id="12">Member A</name></members>
        <groups><name id="13">Group A</name></groups></artist>
        """;

    private const string ArtistC = """
        <artist><images><image type="primary" uri="" uri150="" width="3" height="3"/></images><id>3</id><name>Artist C</name>
        <realname>Real C</realname><profile>Profile C</profile><data_quality>Needs Vote</data_quality>
        <urls><url>http://c.example</url><url>http://c2.example</url></urls><namevariations><name>C Var</name><name>C Var 2</name></namevariations>
        <aliases><name id="31">Alias C</name></aliases><members><id>32</id><name id="32">Member C</name><id>33</id><name id="33">Member C2</name></members>
        <groups><name id="34">Group C</name></groups></artist>
        """;

    public static TheoryData<string, bool> ArtistCases => Cases(
        "<images/>",
        """<images><image type="primary" uri="" uri150="" width="2" height="2"/></images>""",
        "<realname/>",
        "<profile/>",
        "<data_quality/>",
        "<urls/>",
        "<urls><url/></urls>",
        "<urls><url>http://b.example</url><url/></urls>",
        "<urls><url/><url>http://b.example</url></urls>",
        "<namevariations/>",
        "<namevariations><name/></namevariations>",
        "<namevariations><name>B Var</name><name/></namevariations>",
        "<members/>",
        """<members><id>21</id><name id="21"/></members>""",
        """<members><id/><name id="21">Member B</name></members>""",
        "<aliases/>",
        """<aliases><name id="22"/><name id="23">Alias B</name></aliases>""",
        """<aliases><name id="23">Alias B</name><name id="22"/></aliases>""",
        "<groups/>",
        """<groups><name id="24"/></groups>""",
        "<unknown/>",
        "<unknown><nested/></unknown>");

    [Theory]
    [MemberData(nameof(ArtistCases))]
    public async Task Artist_SelfClosingElement_ParsesLikeExplicitElement(string snippet, bool atEnd)
        => await AssertParsedIndependentlyAsync<Artist>("artists", ArtistA,
            $"<artist>{Place("<id>2</id><name>Artist B</name>", snippet, atEnd)}</artist>", ArtistC);

    [Fact]
    public async Task Artist_SelfClosingRecord_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Artist>("artists", ArtistA, "<artist/>", ArtistC);

    [Fact]
    public async Task Artist_SelfClosingName_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Artist>("artists", ArtistA, "<artist><id>2</id><name/></artist>", ArtistC);

    #endregion

    #region labels

    private const string LabelA = """
        <label><images><image type="primary" uri="" uri150="" width="1" height="1"/></images><id>1</id><name>Label A</name>
        <contactinfo>Contact A</contactinfo><profile>Profile A</profile><data_quality>Correct</data_quality>
        <urls><url>http://a.example</url></urls><parentLabel id="10">Parent A</parentLabel>
        <sublabels><label id="11">Sub A</label></sublabels></label>
        """;

    private const string LabelC = """
        <label><images><image type="primary" uri="" uri150="" width="3" height="3"/></images><id>3</id><name>Label C</name>
        <contactinfo>Contact C</contactinfo><profile>Profile C</profile><data_quality>Needs Vote</data_quality>
        <urls><url>http://c.example</url><url>http://c2.example</url></urls><parentLabel id="30">Parent C</parentLabel>
        <sublabels><label id="31">Sub C</label><label id="32">Sub C2</label></sublabels></label>
        """;

    public static TheoryData<string, bool> LabelCases => Cases(
        "<images/>",
        """<images><image type="primary" uri="" uri150="" width="2" height="2"/></images>""",
        "<contactinfo/>",
        "<profile/>",
        "<data_quality/>",
        "<urls/>",
        "<urls><url/></urls>",
        "<urls><url>http://b.example</url><url/></urls>",
        """<parentLabel id="20"/>""",
        "<sublabels/>",
        """<sublabels><label id="21"/></sublabels>""",
        """<sublabels><label id="21"/><label id="22">Sub B</label></sublabels>""",
        """<sublabels><label id="22">Sub B</label><label id="21"/></sublabels>""",
        "<unknown/>",
        "<unknown><nested/></unknown>");

    [Theory]
    [MemberData(nameof(LabelCases))]
    public async Task Label_SelfClosingElement_ParsesLikeExplicitElement(string snippet, bool atEnd)
        => await AssertParsedIndependentlyAsync<Label>("labels", LabelA,
            $"<label>{Place("<id>2</id><name>Label B</name>", snippet, atEnd)}</label>", LabelC);

    [Fact]
    public async Task Label_SelfClosingRecord_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Label>("labels", LabelA, "<label/>", LabelC);

    #endregion

    #region masters

    private const string MasterA = """
        <master id="1"><main_release>10</main_release><images><image type="primary" uri="" uri150="" width="1" height="1"/></images>
        <artists><artist><id>11</id><name>Artist A</name><anv>A</anv><join>,</join><role>Role A</role><tracks>1</tracks></artist></artists>
        <genres><genre>Rock</genre></genres><styles><style>Punk</style></styles><year>2001</year><title>Master A</title>
        <data_quality>Correct</data_quality>
        <videos><video src="https://v/a" duration="1" embed="true"><title>Video A</title><description>Desc A</description></video></videos>
        </master>
        """;

    private const string MasterC = """
        <master id="3"><main_release>30</main_release><images><image type="primary" uri="" uri150="" width="3" height="3"/></images>
        <artists><artist><id>31</id><name>Artist C</name><anv/><join/><role/><tracks/></artist>
        <artist><id>32</id><name>Artist C2</name><anv>C2</anv><join>&amp;</join><role>Role C</role><tracks>2</tracks></artist></artists>
        <genres><genre>Jazz</genre><genre>Funk</genre></genres><styles><style>Bop</style></styles><year>2003</year><title>Master C</title>
        <data_quality>Needs Vote</data_quality>
        <videos><video src="https://v/c1" duration="3" embed="true"><title>Video C1</title><description>Desc C1</description></video>
        <video src="https://v/c2" duration="4" embed="false"><title>Video C2</title><description>Desc C2</description></video></videos>
        </master>
        """;

    private const string FullVideo = """<video src="https://v/b" duration="2" embed="true"><title>Video B</title><description>Desc B</description></video>""";
    private const string EmptyVideo = """<video src="https://v/empty" duration="0" embed="true"/>""";

    private static readonly string[] _videoSnippets =
    [
        "<videos/>",
        $"<videos>{EmptyVideo}</videos>",
        $"<videos>{FullVideo}{EmptyVideo}</videos>",
        $"<videos>{EmptyVideo}{FullVideo}</videos>",
        $"<videos>{FullVideo}{EmptyVideo}{FullVideo}</videos>",
        """<videos><video src="https://v/b" duration="2" embed="true"><title/><description/></video></videos>""",
        """<videos><video src="https://v/b" duration="2" embed="true"><title>Video B</title><description/></video></videos>""",
    ];

    private static readonly string[] _creditArtistSnippets =
    [
        "<artists/>",
        "<artists><artist/></artists>",
        "<artists><artist><id>21</id><name>Artist B</name></artist><artist/></artists>",
        "<artists><artist><id>21</id><name>Artist B</name><anv/><join/><role/><tracks/></artist></artists>",
        "<artists><artist><id/><name/><anv/><join/><role/><tracks/></artist></artists>",
    ];

    public static TheoryData<string, bool> MasterCases => Cases(
    [
        "<main_release/>",
        "<images/>",
        """<images><image type="primary" uri="" uri150="" width="2" height="2"/></images>""",
        "<genres/>",
        "<genres><genre/></genres>",
        "<genres><genre>Pop</genre><genre/></genres>",
        "<styles/>",
        "<styles><style/></styles>",
        "<year/>",
        "<data_quality/>",
        "<unknown/>",
        "<unknown><nested/></unknown>",
        .. _creditArtistSnippets,
        .. _videoSnippets,
    ]);

    [Theory]
    [MemberData(nameof(MasterCases))]
    public async Task Master_SelfClosingElement_ParsesLikeExplicitElement(string snippet, bool atEnd)
        => await AssertParsedIndependentlyAsync<Master>("masters", MasterA,
            $"""<master id="2">{Place("<title>Master B</title>", snippet, atEnd)}</master>""", MasterC);

    [Fact]
    public async Task Master_SelfClosingRecord_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Master>("masters", MasterA, """<master id="2"/>""", MasterC);

    [Fact]
    public async Task Master_SelfClosingTitle_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Master>("masters", MasterA, """<master id="2"><year>2002</year><title/></master>""", MasterC);

    #endregion

    #region releases

    private const string ReleaseA = """
        <release id="1" status="Accepted"><images><image type="primary" uri="" uri150="" width="1" height="1"/></images>
        <artists><artist><id>11</id><name>Artist A</name><anv/><join/><role/><tracks/></artist></artists>
        <title>Release A</title><labels><label name="Label A" catno="A-1" id="12"/></labels>
        <extraartists><artist><id>13</id><name>Extra A</name><anv/><join/><role>Producer</role><tracks/></artist></extraartists>
        <formats><format name="Vinyl" qty="1" text=""><descriptions><description>LP</description></descriptions></format></formats>
        <genres><genre>Rock</genre></genres><styles><style>Punk</style></styles><country>UK</country><released>2001</released>
        <notes>Notes A</notes><data_quality>Correct</data_quality><master_id is_main_release="true">100</master_id>
        <tracklist><track><position>A1</position><title>Track A1</title><duration>1:00</duration>
        <artists><artist><id>14</id><name>Track Artist A</name><anv/><join/><role/><tracks/></artist></artists></track></tracklist>
        <identifiers><identifier type="Barcode" value="111" description="Text"/></identifiers>
        <videos><video src="https://v/a" duration="1" embed="true"><title>Video A</title><description>Desc A</description></video></videos>
        <companies><company><id>15</id><name>Company A</name><catno>CA</catno><entity_type>13</entity_type>
        <entity_type_name>Phonographic Copyright (p)</entity_type_name><resource_url>https://c/a</resource_url></company></companies>
        </release>
        """;

    private const string ReleaseC = """
        <release id="3" status="Accepted"><images><image type="primary" uri="" uri150="" width="3" height="3"/></images>
        <artists><artist><id>31</id><name>Artist C</name><anv>C</anv><join>,</join><role/><tracks/></artist>
        <artist><id>32</id><name>Artist C2</name><anv/><join/><role/><tracks/></artist></artists>
        <title>Release C</title><labels><label name="Label C" catno="C-1" id="33"/><label name="Label C2" catno="C-2" id="34"/></labels>
        <extraartists><artist><id>35</id><name>Extra C</name><anv/><join/><role>Mixed By</role><tracks>A1</tracks></artist></extraartists>
        <formats><format name="CD" qty="2" text="text"><descriptions><description>Album</description><description>Compilation</description></descriptions></format>
        <format name="File" qty="1" text=""/></formats>
        <genres><genre>Jazz</genre><genre>Funk</genre></genres><styles><style>Bop</style></styles><country>US</country><released>2003-01-02</released>
        <notes>Notes C</notes><data_quality>Needs Vote</data_quality><master_id is_main_release="false">300</master_id>
        <tracklist><track><position>1</position><title>Track C1</title><duration>3:00</duration>
        <extraartists><artist><id>36</id><name>Track Extra C</name><anv/><join/><role>Vocals</role><tracks/></artist></extraartists></track>
        <track><position/><title>Track C2</title><duration/><sub_tracks><track><position>2a</position><title>Sub C2a</title><duration>1:00</duration></track>
        <track><position>2b</position><title>Sub C2b</title><duration>2:00</duration></track></sub_tracks></track></tracklist>
        <identifiers><identifier type="Barcode" value="333" description="Text"/><identifier type="Matrix / Runout" value="C-RUN"/></identifiers>
        <videos><video src="https://v/c1" duration="3" embed="true"><title>Video C1</title><description>Desc C1</description></video>
        <video src="https://v/c2" duration="4" embed="false"><title>Video C2</title><description>Desc C2</description></video></videos>
        <companies><company><id>37</id><name>Company C</name><catno/><entity_type>21</entity_type>
        <entity_type_name>Pressed By</entity_type_name><resource_url>https://c/c</resource_url></company></companies>
        </release>
        """;

    private const string TrackB = "<track><position>B1</position><title>Track B1</title><duration>2:00</duration></track>";

    public static TheoryData<string, bool> ReleaseCases => Cases(
    [
        "<images/>",
        """<images><image type="primary" uri="" uri150="" width="2" height="2"/></images>""",
        "<country/>",
        "<released/>",
        "<notes/>",
        "<data_quality/>",
        """<master_id is_main_release="false"/>""",
        "<genres/>",
        "<genres><genre/></genres>",
        "<styles/>",
        "<styles><style>Pop</style><style/></styles>",
        "<labels/>",
        """<labels><label name="Label B" catno="B-1" id="21"/></labels>""",
        """<labels><label name="Label B" catno="B-1" id="21"></label><label name="Label B2" catno="B-2" id="22"/></labels>""",
        "<formats/>",
        """<formats><format name="CD" qty="1" text=""/></formats>""",
        """<formats><format name="CD" qty="1" text=""><descriptions/></format></formats>""",
        """<formats><format name="CD" qty="1" text=""><descriptions><description/></descriptions></format></formats>""",
        """<formats><format name="CD" qty="1" text=""><descriptions><description>Album</description><description/></descriptions></format><format name="File" qty="1" text=""/></formats>""",
        "<identifiers/>",
        """<identifiers><identifier type="Barcode" value="222"/></identifiers>""",
        """<identifiers><identifier type="Barcode" value="222"></identifier><identifier type="Other" value="x"/></identifiers>""",
        "<companies/>",
        "<companies><company/></companies>",
        "<companies><company><id>23</id><name>Company B</name><catno/><entity_type/><entity_type_name/><resource_url/></company></companies>",
        "<companies><company><id>23</id><name>Company B</name></company><company/></companies>",
        "<tracklist/>",
        "<tracklist><track/></tracklist>",
        $"<tracklist>{TrackB}<track/></tracklist>",
        "<tracklist><track><position/><title/><duration/></track></tracklist>",
        "<tracklist><track><position>B1</position><title>Track B1</title><artists/><extraartists/><sub_tracks/></track></tracklist>",
        "<tracklist><track><position>B1</position><title>Track B1</title><artists><artist/></artists></track></tracklist>",
        "<tracklist><track><position>B1</position><title>Track B1</title><extraartists><artist><id>24</id><name>X</name><anv/><join/><role/><tracks/></artist></extraartists></track></tracklist>",
        $"<tracklist><track><position>B</position><title>Track B</title><sub_tracks>{TrackB}<track/></sub_tracks></track>{TrackB}</tracklist>",
        "<unknown/>",
        "<unknown><nested/></unknown>",
        .. _creditArtistSnippets,
        .. _creditArtistSnippets.Select(s => s.Replace("artists>", "extraartists>")),
        .. _videoSnippets,
    ]);

    [Theory]
    [MemberData(nameof(ReleaseCases))]
    public async Task Release_SelfClosingElement_ParsesLikeExplicitElement(string snippet, bool atEnd)
        => await AssertParsedIndependentlyAsync<Release>("releases", ReleaseA,
            $"""<release id="2" status="Accepted">{Place("<title>Release B</title>", snippet, atEnd)}</release>""", ReleaseC);

    [Fact]
    public async Task Release_SelfClosingRecord_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Release>("releases", ReleaseA, """<release id="2" status="Accepted"/>""", ReleaseC);

    [Fact]
    public async Task Release_SelfClosingTitle_ParsesLikeExplicitElement()
        => await AssertParsedIndependentlyAsync<Release>("releases", ReleaseA, """<release id="2" status="Accepted"><country>UK</country><title/></release>""", ReleaseC);

    #endregion
}
