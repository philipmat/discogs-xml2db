using Xunit;

namespace tests
{
    public class DebugOnlyAttribute : FactAttribute
    {
        public DebugOnlyAttribute()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Skip = "Only running in interactive mode.";
            }
        }
    }
}