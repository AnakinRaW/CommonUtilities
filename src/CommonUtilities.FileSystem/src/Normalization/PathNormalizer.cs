using System;
using System.IO;
using System.Runtime.CompilerServices;

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

        path = options.TrailingDirectorySeparatorBehavior switch
        {
            TrailingDirectorySeparatorBehavior.Trim => PathExtensions.TrimTrailingSeparators(path.AsSpan(), options.UnifySeparatorKind),
            TrailingDirectorySeparatorBehavior.Ensure => PathExtensions.EnsureTrailingSeparatorInternal(path),
            _ => path
        };

        // Only do for DirectorySeparatorKind.System, cause for other kinds it will be done at the very end anyway.
        if (options.UnifyDirectorySeparators && options.UnifySeparatorKind == DirectorySeparatorKind.System)
            path = GetPathWithDirectorySeparator(path, DirectorySeparatorKind.System);

        path = NormalizeCasing(path, options.UnifyCase);

        // NB: As previous steps may add new separators (such as GetFullPath) we need to re-apply slash normalization
        // if the desired DirectorySeparatorKind is not DirectorySeparatorKind.System
        if (options.UnifyDirectorySeparators && options.UnifySeparatorKind != DirectorySeparatorKind.System)
            path = GetPathWithDirectorySeparator(path, options.UnifySeparatorKind);

        return path;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeCasing(string path, UnifyCasingKind casing)
    {
        if (casing == UnifyCasingKind.None)
            return path;

        if (!PathExtensions.IsFileSystemCaseInsensitive.Value && !casing.IsForce())
            return path;

        if (casing is UnifyCasingKind.LowerCaseForce or UnifyCasingKind.LowerCase)
            return path.ToLowerInvariant();

        if (casing is UnifyCasingKind.UpperCase or UnifyCasingKind.UpperCaseForce)
            return path.ToUpperInvariant();

        throw new ArgumentOutOfRangeException(nameof(casing));
    }

    private static string GetPathWithDirectorySeparator(string path, DirectorySeparatorKind separatorKind)
    {
        switch (separatorKind)
        {
            case DirectorySeparatorKind.System:
                return PathExtensions.IsUnixLikePlatform ? GetPathWithForwardSlashes(path) : GetPathWithBackSlashes(path);
            case DirectorySeparatorKind.Windows:
                return GetPathWithBackSlashes(path);
            case DirectorySeparatorKind.Linux:
                return GetPathWithForwardSlashes(path);
            default:
                throw new ArgumentOutOfRangeException(nameof(separatorKind));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetPathWithBackSlashes(string path)
    {
        return path.Replace('/', '\\');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetPathWithForwardSlashes(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsForce(this UnifyCasingKind casing)
    {
        return casing is UnifyCasingKind.LowerCaseForce or UnifyCasingKind.UpperCaseForce;
    }
}