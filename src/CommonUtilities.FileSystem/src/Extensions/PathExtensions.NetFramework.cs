#if NET48_OR_GREATER || NETSTANDARD2_0

using System.IO.Abstractions;
using System;
using System.IO;

namespace AnakinRaW.CommonUtilities.FileSystem;

// Mostly copied from https://github.com/dotnet/runtime
public static partial class PathExtensions
{
    // \\?\, \\.\, \??\
    private const int DevicePrefixLength = 4;
    // \\
    private const int UncPrefixLength = 2;
    // \\?\UNC\, \\.\UNC\
    private const int UncExtendedPrefixLength = 8;

    /// <summary>
    /// Determines whether the path represented by the specified character span includes a file name extension.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to search for an extension.</param>
    /// <returns><see langword="true"/> if the characters that follow the last directory separator character or volume separator
    /// in the path include a period (".") followed by one or more characters; otherwise, <see langword="false"/>.</returns>
    public static bool HasExtension(this IPath _, ReadOnlySpan<char> path)
    {
        for (var i = path.Length - 1; i >= 0; i--)
        {
            var ch = path[i];
            if (ch == '.')
                return i != path.Length - 1;
            if (IsAnyDirectorySeparator(ch))
                break;
        }
        return false;
    }

    /// <summary>
    /// Returns the extension of a file path that is represented by a read-only character span.
    /// </summary>
    /// <param name="_">The path.</param>
    /// <param name="path">The file path from which to get the extension.</param>
    /// <returns>The extension of the specified path (including the period, "."), or Empty if <paramref name="path"/> does not have extension information.</returns>
    /// <remarks>
    /// This method obtains the extension of <paramref name="path"/> by searching <paramref name="path"/> for a period ("."),
    /// starting from the last character in the read-only span and continuing toward its first character.
    /// If a period is found before a DirectorySeparatorChar or AltDirectorySeparatorChar character,
    /// the returned read-only span contains the period and the characters after it; otherwise, <see cref="ReadOnlySpan{T}.Empty"/> is returned.
    /// </remarks>
    public static ReadOnlySpan<char> GetExtension(this IPath _, ReadOnlySpan<char> path)
    {
        var length = path.Length;

        for (var i = length - 1; i >= 0; i--)
        {
            var ch = path[i];
            if (ch == '.')
            {
                return i != length - 1
                    ? path.Slice(i, length - i)
                    : ReadOnlySpan<char>.Empty;
            }
            if (IsAnyDirectorySeparator(ch))
                break;
        }
        return ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Returns the file name and extension of a file path that is represented by a read-only character span.
    /// </summary>
    /// <param name="_">The path.</param>
    /// <param name="path">A read-only span that contains the path from which to obtain the file name and extension.</param>
    /// <returns>The characters after the last directory separator character in <paramref name="path"/>.</returns>
    /// <remarks>
    /// Under .NET Framework and .NET Standard 2.0 this method behaves like .NET Core for file names with ":", such as
    /// "C:\file.txt:stream" --> "file.txt:stream" whereas in .NET Framework <see cref="Path.GetFileName"/> the result would be "stream".
    /// </remarks>
    public static ReadOnlySpan<char> GetFileName(this IPath _, ReadOnlySpan<char> path)
    {
        if (IsUnixLikePlatform)
        {
            // While this case should not get reached, we add a safeguard and fallback to the .NET routine.
            return _.GetFileName(path.ToString()).AsSpan();
        }

        var root = _.GetPathRoot(path).Length;

        // We don't want to cut off "C:\file.txt:stream" (i.e. should be "file.txt:stream")
        // but we *do* want "C:Foo" => "Foo". This necessitates checking for the root.

        var i = _.DirectorySeparatorChar == _.AltDirectorySeparatorChar
            ? path.LastIndexOf(_.DirectorySeparatorChar)
            : path.LastIndexOfAny(_.DirectorySeparatorChar, _.AltDirectorySeparatorChar);

        return path.Slice(i < root ? root : i + 1);
    }

    /// <summary>
    /// Returns the characters between the last separator and last (.) in the path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(this IPath _, ReadOnlySpan<char> path)
    {
        var fileName = _.GetFileName(path);
        var lastPeriod = fileName.LastIndexOf('.');
        return lastPeriod < 0 ?
            fileName : // No extension was found
            fileName.Slice(0, lastPeriod);
    }

    /// <summary>
    /// Gets the root directory information from the path contained in the specified character span.
    /// </summary>
    /// <param name="_">The path.</param>
    /// <param name="path">A read-only span of characters containing the path from which to obtain root directory information.</param>
    /// <returns>A read-only span of characters containing the root directory of <paramref name="path"/>.</returns>
    public static ReadOnlySpan<char> GetPathRoot(this IPath _, ReadOnlySpan<char> path)
    {
        if (IsUnixLikePlatform)
        {
            // While this case should not get reached, we add a safeguard and fallback to the .NET routine.
            return _.GetPathRoot(path.ToString()).AsSpan();
        }

        if (IsEffectivelyEmpty(path))
            return ReadOnlySpan<char>.Empty;

        var pathRoot = GetRootLength(path);
        return pathRoot <= 0 ? ReadOnlySpan<char>.Empty : path.Slice(0, pathRoot);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the path specified is absolute. This method does no
    /// validation of the path.
    /// </summary>
    /// <remarks>
    /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
    /// assume that rooted paths (Path.IsPathRooted) are not relative. This isn't the case.
    /// "C:a" is drive relative-meaning that it will be resolved against the current directory
    /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
    /// will not be used to modify the path).
    /// </remarks>
    /// <param name="_">The path.</param>
    /// <param name="path">A file path.</param>
    /// <returns><see langword="true"/> if the path is fixed to a specific drive or UNC path; <see langword="false"/> if the path is relative to the current drive or working directory.</returns>
    public static bool IsPathFullyQualified(this IPath _, ReadOnlySpan<char> path)
    {
        if (IsUnixLikePlatform)
        {
            // While this case should not get reached, we add a safeguard and fallback to the .NET routine.
            return _.IsPathRooted(path.ToString());
        }

        if (path.Length < 2)
            return false;

        if (IsAnyDirectorySeparator(path[0]))
            return path[1] == '?' || IsAnyDirectorySeparator(path[1]);

        return path.Length >= 3
               && path[1] == VolumeSeparatorChar
               && IsAnyDirectorySeparator(path[2])
               && IsValidDriveChar(path[0]);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the path specified is absolute. This method does no
    /// validation of the path.
    /// </summary>
    /// <remarks>
    /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
    /// assume that rooted paths (Path.IsPathRooted) are not relative. This isn't the case.
    /// "C:a" is drive relative-meaning that it will be resolved against the current directory
    /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
    /// will not be used to modify the path).
    /// </remarks>
    /// <param name="_">The path.</param>
    /// <param name="path">A file path.</param>
    /// <returns><see langword="true"/> if the path is fixed to a specific drive or UNC path; <see langword="false"/> if the path is relative to the current drive or working directory.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/>is <see langword="null"/>.</exception>
    public static bool IsPathFullyQualified(this IPath _, string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        return IsPathFullyQualified(_, path.AsSpan());
    }

    /// <summary>
    /// Returns a value that indicates whether a file path contains a root.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">The path to test.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> contains a root; otherwise, <see langword="false"/>.</returns>
    public static bool IsPathRooted(this IPath _, ReadOnlySpan<char> path)
    {
        var length = path.Length;
        return (length >= 1 && IsAnyDirectorySeparator(path[0]))
               || (length >= 2 && IsValidDriveChar(path[0]) && path[1] == VolumeSeparatorChar);
    }

    /// <summary>
    /// Returns the directory information for the specified path represented by a character span.
    /// </summary>
    /// <remarks>
    /// Unlike the string overload, this method doesn't normalize directory separators.
    /// </remarks>
    /// <param name="_">The path.</param>
    /// <param name="path">The path to retrieve the directory information from.</param>
    /// <returns>
    /// Directory information for <paramref name="path"/>, or an empty span if <paramref name="path"/> is <see langword="null"/>,
    /// an empty span, or a root (such as \, C:, or \server\share).
    /// </returns>
    public static ReadOnlySpan<char> GetDirectoryName(this IPath _, ReadOnlySpan<char> path)
    {
        if (IsEffectivelyEmpty(path))
            return ReadOnlySpan<char>.Empty;

        var end = GetDirectoryNameOffset(path);
        return end >= 0 ? path.Slice(0, end) : ReadOnlySpan<char>.Empty;
    }

    internal static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        int rootLength = GetRootLength(path);
        int end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsAnyDirectorySeparator(path[--end])) ;

        // Trim off any remaining separators (to deal with C:\foo\\bar)
        while (end > rootLength && IsAnyDirectorySeparator(path[end - 1]))
            end--;

        return end;
    }

    internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
            return true;

        foreach (var c in path)
        {
            if (c != ' ')
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the length of the root of the path (drive, share, etc.).
    /// </summary>
    internal static int GetRootLength(ReadOnlySpan<char> path)
    {
        var pathLength = path.Length;
        var i = 0;

        var deviceSyntax = IsDevice(path);
        var deviceUnc = deviceSyntax && IsDeviceUNC(path);

        if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsAnyDirectorySeparator(path[0]))
        {
            // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")
            if (deviceUnc || (pathLength > 1 && IsAnyDirectorySeparator(path[1])))
            {
                // UNC (\\?\UNC\ or \\), scan past server\share

                // Start past the prefix ("\\" or "\\?\UNC\")
                i = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;

                // Skip two separators at most
                var n = 2;
                while (i < pathLength && (!IsAnyDirectorySeparator(path[i]) || --n > 0))
                    i++;
            }
            else
            {
                // Current drive rooted (e.g. "\foo")
                i = 1;
            }
        }
        else if (deviceSyntax)
        {
            // Device path (e.g. "\\?\.", "\\.\")
            // Skip any characters following the prefix that aren't a separator
            i = DevicePrefixLength;
            while (i < pathLength && !IsAnyDirectorySeparator(path[i]))
                i++;

            // If there is another separator take it, as long as we have had at least one
            // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
            if (i < pathLength && i > DevicePrefixLength && IsAnyDirectorySeparator(path[i]))
                i++;
        }
        else if (pathLength >= 2
            && path[1] == VolumeSeparatorChar
            && IsValidDriveChar(path[0]))
        {
            // Valid drive specified path ("C:", "D:", etc.)
            i = 2;

            // If the colon is followed by a directory separator, move past it (e.g "C:\")
            if (pathLength > 2 && IsAnyDirectorySeparator(path[2]))
                i++;
        }
        return i;
    }

    /// <summary>
    /// Returns true if the path uses any of the DOS device path syntaxes. ("\\.\", "\\?\", or "\??\")
    /// </summary>
    private static bool IsDevice(ReadOnlySpan<char> path)
    {
        // If the path begins with any two separators is will be recognized and normalized and prepped with
        // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
        return IsExtended(path)
               ||
               (
                   path.Length >= DevicePrefixLength
                   && IsAnyDirectorySeparator(path[0])
                   && IsAnyDirectorySeparator(path[1])
                   && (path[2] == '.' || path[2] == '?')
                   && IsAnyDirectorySeparator(path[3])
               );
    }

    /// <summary>
    /// Returns true if the path is a device UNC (\\?\UNC\, \\.\UNC\)
    /// </summary>
    private static bool IsDeviceUNC(ReadOnlySpan<char> path)
    {
        return path.Length >= UncExtendedPrefixLength
               && IsDevice(path)
               && IsAnyDirectorySeparator(path[7])
               && path[4] == 'U'
               && path[5] == 'N'
               && path[6] == 'C';
    }

    /// <summary>
    /// Returns true if the path uses the canonical form of extended syntax ("\\?\" or "\??\"). If the
    /// path matches exactly (cannot use alternate directory separators) Windows will skip normalization
    /// and path length checks.
    /// </summary>
    private static bool IsExtended(ReadOnlySpan<char> path)
    {
        // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
        // Skipping of normalization will *only* occur if back slashes ('\') are used.
        return path.Length >= DevicePrefixLength
               && path[0] == '\\'
               && (path[1] == '\\' || path[1] == '?')
               && path[2] == '?'
               && path[3] == '\\';
    }
}

#endif