#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill required so this library can use C# 9 <c>init</c>-only properties while still
/// targeting netstandard2.0 (which doesn't ship this marker type in its BCL). Not part of the
/// library's public API - it just makes the compiler happy.
/// </summary>
internal static class IsExternalInit
{
}
#endif
