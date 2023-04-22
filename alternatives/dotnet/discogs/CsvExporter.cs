using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace discogs
{
    public interface IExporter<T> : IDisposable
        where T : IExportable, new()
    {
        Task ExportAsync(T value);
        Task CompleteExportAsync(int finalCount);
    }

    public class CsvExporter<T> : IExporter<T>
        where T : IExportable, new()
    {
        private const int BufferSize = 1024 * 1024;
        private readonly string _typeName;
        private readonly Dictionary<string, (string FilePath, StreamWriter FileStream)> _csvStreams;
        private bool disposedValue;

        public CsvExporter(string outPutDirectory, bool compress = false, bool verbose = false)
        {
            _typeName = typeof(T).Name.Split('.')[^1];
            _csvStreams = GetCsvFilesFor(outPutDirectory, compress);
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
            IEnumerable<(string StreamName, string[] Row)> csvExports = value.Export();
            foreach (var (streamName, row) in csvExports)
            {
                await _csvStreams[streamName].FileStream.WriteLineAsync(CsvExtensions.ToCsv(row));
            }
        }

        private static Dictionary<string, (string FilePath, StreamWriter FileStream)> GetCsvFilesFor(string outPutDirectory, bool compress)
        {
            var obj = new T();
            IReadOnlyDictionary<string, string[]> files = obj.GetExportStreamsAndFields();
            Dictionary<string, (string FilePath, StreamWriter FileStream)> csvFiles = files.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var extension = compress ? "csv.gz" : "csv";
                    var csvFile = Path.Combine(outPutDirectory, $"{kvp.Key}.{extension}");
                    StreamWriter stream;
                    if (compress)
                    {
                        var fs = File.Create(csvFile, bufferSize: BufferSize);
                        var gzStream = new GZipStream(fs, CompressionMode.Compress, leaveOpen: false);
                        stream = new StreamWriter(gzStream, encoding: System.Text.Encoding.UTF8);
                    }
                    else
                    {
                        stream = new StreamWriter(csvFile, append: false, encoding: System.Text.Encoding.UTF8, bufferSize: BufferSize);
                    }
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
                    foreach (var kvp in _csvStreams)
                    {
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