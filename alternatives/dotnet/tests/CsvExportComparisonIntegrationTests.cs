using System.IO.Compression;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;

namespace tests;

public class CsvExportComparisonIntegrationTests : IDisposable
{
    private static readonly Lazy<string> _samplesDirectory =
        new(() => FindResourcesDirectory("samples"));
    private static readonly Lazy<string> _exportDirectory =
        new(() => FindResourcesDirectory("export"));

    private readonly string _outputRoot;

    public CsvExportComparisonIntegrationTests()
    {
        _outputRoot = Path.Combine(Path.GetTempPath(), "DiscogsCsvComparison");
        if (Directory.Exists(_outputRoot))
        {
            Directory.Delete(_outputRoot, recursive: true);
        }
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputRoot))
        {
            Directory.Delete(_outputRoot, recursive: true);
        }
    }

    public static IEnumerable<object[]> SampleFiles()
    {
        string samplesDirectory = _samplesDirectory.Value;
        IEnumerable<string> files = Directory.EnumerateFiles(samplesDirectory, "discogs_*_*.xml.gz");
        foreach (string file in files)
        {
            yield return [file];
        }
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public async Task ParseSample_ExportsMatchExpectedCsvAsync(string sampleFile)
    {
        // Given
        string exportDirectory = _exportDirectory.Value;
        Directory.Exists(exportDirectory).Should().BeTrue(
            because: $"expected export directory at '{exportDirectory}'");

        string baseName = GetSampleBaseName(sampleFile);
        string outputDirectory = Path.Combine(_outputRoot, baseName);
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
        Directory.CreateDirectory(outputDirectory);

        Type entityType = GetEntityTypeForSample(sampleFile);
        object exportable = Activator.CreateInstance(entityType)
            ?? throw new InvalidOperationException($"Could not create instance of '{entityType.FullName}'.");
        IReadOnlyDictionary<string, string[]> exportScheme = GetExportScheme(exportable);
        Dictionary<string, string> expectedFilesByStream = GetExpectedCsvFilesByStream(exportDirectory);

        // When
        await ParseAndExportAsync(entityType, sampleFile, outputDirectory);

        // Then
        List<string> differences = [];
        foreach (var (streamName, headers) in exportScheme)
        {
            string actualPath = Path.Combine(outputDirectory, $"{streamName}.csv");
            if (!File.Exists(actualPath))
            {
                differences.Add($"Missing actual csv file for '{streamName}' at '{actualPath}'.");
                continue;
            }

            if (!expectedFilesByStream.TryGetValue(streamName, out string expectedPath))
            {
                differences.Add($"Missing expected csv file for '{streamName}' in '{exportDirectory}'.");
                continue;
            }

            IReadOnlyList<string> fileDifferences = CompareCsvFiles(
                streamName,
                actualPath,
                expectedPath,
                headers.Length);
            if (fileDifferences.Count > 0)
            {
                differences.AddRange(fileDifferences);
            }
        }

        differences.Should().BeEmpty(
            because: $"CSV comparison differences:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
    }

    private static async Task ParseAndExportAsync(Type entityType, string sampleFile, string outputDirectory)
    {
        await using FileStream fileStream = File.OpenRead(sampleFile);
        await using Stream inputStream = CreateInputStream(fileStream, sampleFile);

        Type exporterType = typeof(CsvExporter<>).MakeGenericType(entityType);
        object exporter = Activator.CreateInstance(exporterType, outputDirectory, false, false)
            ?? throw new InvalidOperationException($"Could not create '{exporterType.FullName}'.");

        try
        {
            Type parserType = typeof(Parser<>).MakeGenericType(entityType);
            object parser = Activator.CreateInstance(parserType, exporter, 1)
                ?? throw new InvalidOperationException($"Could not create '{parserType.FullName}'.");
            MethodInfo parseMethod = parserType.GetMethod("ParseStreamAsync")
                ?? throw new InvalidOperationException($"'{parserType.FullName}' does not have ParseStreamAsync.");

            Task parseTask = (Task)parseMethod.Invoke(parser, [inputStream])
                ?? throw new InvalidOperationException("ParseStreamAsync did not return a task.");
            await parseTask;
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static Stream CreateInputStream(Stream fileStream, string sampleFile)
    {
        if (sampleFile.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            return new GZipStream(fileStream, CompressionMode.Decompress);
        }

        return fileStream;
    }

    private static IReadOnlyDictionary<string, string[]> GetExportScheme(object exportable)
    {
        MethodInfo method = exportable.GetType().GetMethod("GetExportStreamsAndFields");
        if (method != null)
        {
            return (IReadOnlyDictionary<string, string[]>)method.Invoke(exportable, null);
        }

        if (exportable is IExportable exportToCsv)
        {
            return exportToCsv.GetExportStreamsAndFields();
        }

        throw new InvalidOperationException(
            $"Exportable type '{exportable.GetType().FullName}' does not expose an export scheme method.");
    }

    private static IReadOnlyList<string> CompareCsvFiles(
        string streamName,
        string actualPath,
        string expectedPath,
        int expectedHeaderCount)
    {
        CsvFileStats actualStats = GetCsvFileStats(actualPath);
        CsvFileStats expectedStats = GetCsvFileStats(expectedPath);

        List<string> differences = [];
        if (actualStats.HeaderCount != expectedHeaderCount)
        {
            differences.Add(
                $"{streamName}: actual header count {actualStats.HeaderCount} does not match schema {expectedHeaderCount}.");
        }

        if (expectedStats.HeaderCount != expectedHeaderCount)
        {
            differences.Add(
                $"{streamName}: expected header count {expectedStats.HeaderCount} does not match schema {expectedHeaderCount}.");
        }

        if (actualStats.HeaderCount != expectedStats.HeaderCount)
        {
            differences.Add(
                $"{streamName}: header count actual {actualStats.HeaderCount} vs expected {expectedStats.HeaderCount}.");
        }

        if (actualStats.LineCount != expectedStats.LineCount)
        {
            differences.Add(
                $"{streamName}: line count actual {actualStats.LineCount} vs expected {expectedStats.LineCount}.");
        }

        if (actualStats.RecordCount != expectedStats.RecordCount)
        {
            differences.Add(
                $"{streamName}: record count actual {actualStats.RecordCount} vs expected {expectedStats.RecordCount}.");
        }

        return differences;
    }

    private static CsvFileStats GetCsvFileStats(string path)
    {
        int lineCount = CountLines(path);
        using Stream stream = OpenCsvStream(path);
        using var parser = new TextFieldParser(stream)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return new CsvFileStats(0, lineCount, 0);
        }

        string[] header = parser.ReadFields() ?? [];
        int recordCount = 0;
        while (!parser.EndOfData)
        {
            parser.ReadFields();
            recordCount += 1;
        }

        return new CsvFileStats(header.Length, lineCount, recordCount);
    }

    private static int CountLines(string path)
    {
        using Stream stream = OpenCsvStream(path);
        using var reader = new StreamReader(stream);
        int count = 0;
        while (reader.ReadLine() is not null)
        {
            count += 1;
        }
        return count;
    }

    private static Stream OpenCsvStream(string path)
    {
        FileStream fileStream = File.OpenRead(path);
        if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            return new GZipStream(fileStream, CompressionMode.Decompress);
        }

        return fileStream;
    }

    private static string GetSampleBaseName(string sampleFile)
    {
        string fileName = Path.GetFileName(sampleFile);
        if (fileName.EndsWith(".xml.gz", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^7];
        }

        if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^4];
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static Type GetEntityTypeForSample(string sampleFile)
    {
        string fileName = Path.GetFileName(sampleFile);
        string entityToken = GetEntityToken(fileName);
        string singular = GetSingularEntity(entityToken);
        string pascal = ToPascalCase(singular);

        Type[] types = typeof(IExportable).Assembly.GetTypes();
        List<string> candidateNames = [$"Discogs{pascal}", singular];

        foreach (string candidate in candidateNames)
        {
            Type match = types.FirstOrDefault(
                t => typeof(IExportable).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && t.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        throw new InvalidOperationException(
            $"No exportable type found for '{entityToken}' (candidates: {string.Join(", ", candidateNames)}).");
    }

    private static string GetEntityToken(string fileName)
    {
        string name = fileName;
        if (name.EndsWith(".xml.gz", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^7];
        }
        else if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        string[] parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            throw new InvalidOperationException($"Sample file name '{fileName}' does not match discogs_<date>_<name>.");
        }

        return parts[^1];
    }

    private static string GetSingularEntity(string token)
        => token.ToLowerInvariant() switch
        {
            "artists" => "artist",
            "labels" => "label",
            "masters" => "master",
            "releases" => "release",
            _ => token,
        };

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(char.ToUpperInvariant(value[0]), value.Substring(1));
    }

    private static Dictionary<string, string> GetExpectedCsvFilesByStream(string exportDirectory)
    {
        IEnumerable<string> csvFiles = Directory.EnumerateFiles(exportDirectory, "*.csv*");
        Dictionary<string, string> filesByStream = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in csvFiles)
        {
            string streamName = GetStreamNameFromFile(file);
            if (!filesByStream.ContainsKey(streamName))
            {
                filesByStream[streamName] = file;
            }
        }

        return filesByStream;
    }

    private static string GetStreamNameFromFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith(".csv.gz", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^7];
        }

        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^4];
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static string FindResourcesDirectory(string resourceName)
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "alternatives",
                "dotnet",
                "tests",
                "Resources",
                resourceName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate alternatives/dotnet/tests/Resources/{resourceName} starting at '{AppContext.BaseDirectory}'.");
    }

    private readonly record struct CsvFileStats(int HeaderCount, int LineCount, int RecordCount);
}
