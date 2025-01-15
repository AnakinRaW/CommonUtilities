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
    /// Normalizes the path contained in the specified character span according to specified normalization rules.
    /// </summary>
    /// <param name="path">A read-only span of characters containing the path to normalize.</param>
    /// <param name="options">The options how to normalize.</param>
    /// <returns>The normalized path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="IOException">The normalization failed due to an internal error.</exception>
    public static string Normalize(string path, in PathNormalizeOptions options)
    {
        return Normalize(path.AsSpan(), options);
    }

    /// <summary>
    /// Normalizes the specified path according to the specified normalization rules.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="options">The options how to normalize.</param>
    /// <returns>The normalized path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="IOException">The normalization failed due to an internal error.</exception>
    public static string Normalize(ReadOnlySpan<char> path, in PathNormalizeOptions options)
    {
        var stringBuilder = new ValueStringBuilder(stackalloc char[PathExtensions.MaxShortPath]);
        Normalize(path, ref stringBuilder, options);
        var result = stringBuilder.ToString();
        stringBuilder.Dispose();
        return result;
    }

    /// <summary>
    /// Normalizes the path contained in the specified character span to a preallocated character span with the specified normalization rules.
    /// </summary>
    /// <param name="path">A read-only span of characters containing the path to normalize.</param>
    /// <param name="destination">The destination span which contains the normalized path.</param>
    /// <param name="options">The options how to normalize.</param>
    /// <returns>The number of characters written into the destination span.</returns>
    /// <remarks>This method populates <paramref name="destination"/> even if <paramref name="destination"/> is too small.</remarks>
    /// <exception cref="ArgumentException">If the <paramref name="destination"/> is too small.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="IOException">The normalization failed due to an internal error.</exception>
    public static int Normalize(ReadOnlySpan<char> path, Span<char> destination, in PathNormalizeOptions options)
    {
        var stringBuilder = new ValueStringBuilder(destination);
        Normalize(path, ref stringBuilder, options);
        if (!stringBuilder.TryCopyTo(destination, out var charsWritten))
            throw new ArgumentException("Cannot copy to destination span", nameof(destination));
        return charsWritten;
    }

    internal static void Normalize(ReadOnlySpan<char> path, ref ValueStringBuilder sb, in PathNormalizeOptions options)
    {
        if (path == ReadOnlySpan<char>.Empty)
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
            NormalizeDirectorySeparator(sb.RawChars, in options);

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

    private static void NormalizeDirectorySeparator(Span<char> pathSpan, in PathNormalizeOptions options)
    {
        if (PathExtensions.IsUnixLikePlatform && options is
            {
                TreatBackslashAsSeparator: false,
                UnifySeparatorKind: DirectorySeparatorKind.Linux or DirectorySeparatorKind.System
            })
            return;

        var separatorChar = GetSeparatorChar(options.UnifySeparatorKind);

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