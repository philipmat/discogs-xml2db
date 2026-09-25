namespace discogs;

internal class RecordCounter<T>(bool verbose = false) : IExporter<T>
    where T : IExportable, new()
{
    private readonly bool _verbose = verbose;
    private readonly Dictionary<string, int> _counter = GetSchemeCounts();

    public Task CompleteExportAsync(int finalCount)
    {
        Console.WriteLine($"Would write {finalCount:n0} parsed records across the following files:");
        int maxFile = _counter.Max(kvp => kvp.Key.Length);
        int maxNum = _counter.Max(kvp => $"{kvp.Value:n0}".Length);
        foreach (var kvp in _counter.OrderBy(kvp => kvp.Key))
        {
            string file = kvp.Key.PadLeft(maxFile);
            string num = kvp.Value.ToString("n0").PadLeft(maxNum);
            Console.WriteLine($" -  {file} : {num}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public Task ExportAsync(T value)
    {
        foreach (var (stream, _) in value.Export())
        {
            _counter[stream] += 1;
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, int> GetSchemeCounts()
    {
        var obj = new T();
        IReadOnlyDictionary<string, string[]> files = obj.GetExportStreamsAndFields();
        return files.ToDictionary(kvp => kvp.Key, _ => 0);
    }
}
