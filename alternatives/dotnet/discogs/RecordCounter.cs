using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace discogs
{

    internal class RecordCounter<T> : IExporter<T>
        where T : IExportable, new()
    {
        private readonly bool _verbose;
        private readonly Dictionary<string, int> _counter;

        public RecordCounter(bool verbose = false)
        {
            _verbose = verbose;
            _counter = GetSchemeCounts();
        }

        public Task CompleteExportAsync(int finalCount)
        {
            Console.WriteLine($"Would write {finalCount:n0} parsed records across the following files:");
            var maxFile = _counter.Max(kvp => kvp.Key.Length);
            var maxNum = _counter.Max(kvp => $"{kvp.Value:n0}".Length);
            foreach (var kvp in _counter.OrderBy(kvp => kvp.Key))
            {
                var file = kvp.Key.PadLeft(maxFile);
                var num = kvp.Value.ToString("n0").PadLeft(maxNum);
                Console.WriteLine($" -  {file} : {num}");
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);
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
            return files.ToDictionary(kvp => kvp.Key, kvp => 0);
        }


    }
}