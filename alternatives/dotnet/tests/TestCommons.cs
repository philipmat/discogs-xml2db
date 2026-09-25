using System.Reflection;

namespace tests;

internal static class TestCommons
{
    private const string ResourceNamespace = "tests.Resources";
    private static readonly Lazy<Assembly> _thisAssembly = new(() => typeof(TestCommons).Assembly);

    internal static async Task<string> GetResourceAsync(string name)
    {
        await using Stream resStream = _thisAssembly.Value.GetManifestResourceStream($"{ResourceNamespace}.{name}");
        if (resStream == null)
        {
            return null;
        }

        using var reader = new StreamReader(resStream);
        return await reader.ReadToEndAsync();
    }

    internal static Stream GetResourceStream(string name)
        => _thisAssembly.Value.GetManifestResourceStream($"{ResourceNamespace}.{name}");
}
