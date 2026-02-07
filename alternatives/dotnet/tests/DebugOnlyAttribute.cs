using System.Runtime.CompilerServices;

namespace tests;

public class DebugOnlyAttribute : FactAttribute
{
    public DebugOnlyAttribute(
        [CallerFilePath]
        string? sourceFilePath = null,
        [CallerLineNumber]
        int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            Skip = "Only running in interactive mode.";
        }
    }
}
