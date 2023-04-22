using System;
using System.IO.Abstractions;
using System.IO;

namespace AnakinRaW.CommonUtilities.Verification;

internal static class Utilities
{
    public static string? GetPathFromStream(this Stream stream)
    {
        return stream switch
        {
            FileStream fileStream => fileStream.Name,
            FileSystemStream fileSystemStream => fileSystemStream.Name,
            _ => null
        };
    }

#if NETSTANDARD
    public static bool IsAssignableTo(this Type type, Type other)
    {
        return other.IsAssignableFrom(type);
    }
#endif
}