using System;
using System.IO;
using System.Runtime.CompilerServices;
using AnakinRaW.CommonUtilities.FileSystem.Utilities;

namespace AnakinRaW.CommonUtilities.FileSystem.Normalization;

/// <summary>
/// Enables customized path normalization.
/// </summary>
public static class PathNormalizer
{
    /// <summary>
    /// Normalizes a given path according to given normalization rules.
    /// </summary>
    /// <param name="path">The input path.</param>
    /// <param name="options">The options how to normalize.</param>
    /// <returns>The normalized path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="IOException">The normalization failed due to an internal error.</exception>
    public static string Normalize(string path, PathNormalizeOptions options)
    {
        var stringBuilder = new ValueStringBuilder(stackalloc char[PathExtensions.MaxShortPath]);
        Normalize(path.AsSpan(), ref stringBuilder, options);
        var result = stringBuilder.ToString();
        stringBuilder.Dispose();
        return result;
    }

    /// <summary>
    /// Normalizes a given character span that represents a file path to a destination according to given normalization rules.
    /// </summary>
    /// <param name="path">The path to normalize</param>
    /// <param name="destination">The destination span which contains the normalized path.</param>
    /// <param name="options">The options how to normalize.</param>
    /// <returns>The number of characters written into the destination span.</returns>
    /// <remarks>This method populates <paramref name="destination"/> even if <paramref name="destination"/> is too small.</remarks>
    /// <exception cref="ArgumentException">If the <paramref name="destination"/> is too small.</exception>
    public static int Normalize(ReadOnlySpan<char> path, Span<char> destination, PathNormalizeOptions options)
    {
        var stringBuilder = new ValueStringBuilder(destination);
        Normalize(path, ref stringBuilder, options);
        if (!stringBuilder.TryCopyTo(destination, out var charsWritten))
            throw new ArgumentException("Cannot copy to destination span", nameof(destination));
        return charsWritten;
    }

    internal static void Normalize(ReadOnlySpan<char> path, ref ValueStringBuilder sb, PathNormalizeOptions options)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException(nameof(path));
        
        switch (options.TrailingDirectorySeparatorBehavior)
        {
            case TrailingDirectorySeparatorBehavior.Trim:
                var trimmedPath = PathExtensions.TrimTrailingSeparators(path, options.UnifySeparatorKind);
                sb.Append(trimmedPath);
                break;
            case TrailingDirectorySeparatorBehavior.Ensure:
                sb.Append(path);
                PathExtensions.EnsureTrailingSeparatorInternal(ref sb);
                break;
            case TrailingDirectorySeparatorBehavior.None:
                sb.Append(path);
                break;
            default:
                throw new IndexOutOfRangeException();
        }

        // As the trailing directory normalization might add new separators, this step must come after.
        if (options.UnifyDirectorySeparators)
            GetPathWithDirectorySeparator(sb.RawChars, options.UnifySeparatorKind);

        NormalizeCasing(sb.RawChars, options.UnifyCase);
    }

    private static bool RequiresCasing(UnifyCasingKind casingOption)
    {
        if (casingOption == UnifyCasingKind.None)
            return false;
        if (!PathExtensions.IsFileSystemCaseInsensitive.Value && !casingOption.IsForce())
            return false;
        return true;
    }

    private static unsafe void NormalizeCasing(Span<char> path, UnifyCasingKind casing)
    {
        if (!RequiresCasing(casing))
            return;

        delegate*<char, char> transformation;
        if (casing is UnifyCasingKind.LowerCase or UnifyCasingKind.LowerCaseForce)
            transformation = &ToLower;
        else
            transformation = &ToUpper;

        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            path[i] = transformation(c);
        }
    }

    private static char ToLower(char c)
    {
        return char.ToLowerInvariant(c);
    }

    private static char ToUpper(char c)
    {
        return char.ToUpperInvariant(c);
    }

    private static void GetPathWithDirectorySeparator(Span<char> pathSpan, DirectorySeparatorKind separatorKind)
    {
        var separatorChar = GetSeparatorChar(separatorKind);

        for (var i = 0; i < pathSpan.Length; i++)
        {
            var c = pathSpan[i];
            if (c is '\\' or '/')
                pathSpan[i] = separatorChar;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char GetSeparatorChar(DirectorySeparatorKind separatorKind)
    {
        return separatorKind switch
        {
            DirectorySeparatorKind.System => PathExtensions.IsUnixLikePlatform ? '/' : '\\',
            DirectorySeparatorKind.Windows => '\\',
            DirectorySeparatorKind.Linux => '/',
            _ => throw new ArgumentOutOfRangeException(nameof(separatorKind))
        };
    }

    private static bool IsForce(this UnifyCasingKind casing)
    {
        return casing is UnifyCasingKind.LowerCaseForce or UnifyCasingKind.UpperCaseForce;
    }
}