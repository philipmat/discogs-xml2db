using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace discogs
{
    public interface IExporter<T> : IDisposable
        where T : IExportToCsv, new()
    {
        Task ExportAsync(T value);
        Task CompleteExportAsync(int finalCount);
    }

    public class CsvExporter<T> : IExporter<T>
        where T : IExportToCsv, new()
    {
        private readonly string _typeName;
        private readonly Dictionary<string, (string FilePath, StreamWriter FileStream)> _csvStreams;
        private bool disposedValue;

        public CsvExporter(string outPutDirectory)
        {
            _typeName = typeof(T).Name.Split('.')[^1];
            _csvStreams = GetCsvFilesFor(outPutDirectory);
        } 
        public async Task CompleteExportAsync(int finalCount)
        {
            var csvFileNames = string.Join("; ", _csvStreams.Select(kvp => kvp.Value.FilePath));
            // pbar.WriteLine("Parsing done. Writing streams.");
            foreach (var kvp in _csvStreams)
            {
                await kvp.Value.FileStream.FlushAsync();
                kvp.Value.FileStream.Close();
                // await kvp.Value.FileStream.DisposeAsync();
            }
            Console.WriteLine($"Found {finalCount:n0} {_typeName}s. Wrote them to {csvFileNames}.");
        }

        public async Task ExportAsync(T value)
        {
            IEnumerable<(string StreamName, string[] Row)> csvExports = value.ExportToCsv();
            foreach (var (streamName, row) in csvExports)
            {
                await _csvStreams[streamName].FileStream.WriteLineAsync(CsvExtensions.ToCsv(row));
            }
        }

        private static Dictionary<string, (string FilePath, StreamWriter FileStream)> GetCsvFilesFor(string outPutDirectory)
        {
            var obj = new T();
            IReadOnlyDictionary<string, string[]> files = obj.GetCsvExportScheme();
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvFiles = files.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var csvFile = Path.Combine(outPutDirectory, $"{kvp.Key}.csv");
                    var stream = new StreamWriter(csvFile);
                    stream.WriteLine(CsvExtensions.ToCsv(kvp.Value));
                    return (csvFile, stream);
                });

            return csvFiles;
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CsvExporter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    foreach( var kvp in _csvStreams) {
                        var (_, stream) = kvp.Value;
                        stream.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
    }
}