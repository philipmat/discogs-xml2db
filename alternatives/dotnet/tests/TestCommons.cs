using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace tests
{
    internal static class TestCommons
    {
        public const string ResourceNamespace = "tests.Resources";
        private static readonly Lazy<Assembly> ThisAssembly = new Lazy<Assembly>(() => typeof(TestCommons).Assembly);

        internal static async Task<string> GetResourceAsync(string name)
        {
            using Stream resStream = ThisAssembly.Value.GetManifestResourceStream($"{ResourceNamespace}.{name}");
            using var reader = new StreamReader(resStream);
            return await reader.ReadToEndAsync();
        }

        internal static Stream GetResourceStream(string name)
            => ThisAssembly.Value.GetManifestResourceStream($"{ResourceNamespace}.{name}");
    }
}