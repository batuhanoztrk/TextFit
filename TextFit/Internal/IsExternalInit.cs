// Polyfill so we can use C# 9 `init` setters when targeting frameworks older than .NET 5.
// The compiler only requires this type to exist; it has nobody.

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
