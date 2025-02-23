#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.Diagnostics;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.FileSystem.Utilities;

namespace AnakinRaW.CommonUtilities.FileSystem;

// Mostly copied from https://github.com/dotnet/runtime
public static partial class PathExtensions
{
	/// <summary>
	/// Concatenates two paths into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates <paramref name="path1"/> and <paramref name="path2"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the length of any of <paramref name="path1"/> and <paramref name="path2"/>
	/// argument is zero, the method concatenates the remaining arguments.
	/// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string,string)"/> method,
	/// the <see cref="Join(IPath,string?,string?)"/> method does not attempt to root the returned path.
	/// (That is, if <paramref name="path2"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string,string)"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">The first path to join.</param>
	/// <param name="path2">The second path to join.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, string? path1, string? path2)
    {
        if (string.IsNullOrEmpty(path1))
            return path2 ?? string.Empty;
        if (string.IsNullOrEmpty(path2))
            return path1!;
        return JoinInternal(path1.AsSpan(), path2.AsSpan());
	}

	/// <summary>
	/// Concatenates two path components into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates <paramref name="path1"/> and <paramref name="path2"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the length of any of <paramref name="path1"/> and <paramref name="path2"/>
	/// argument is zero, the method concatenates the remaining arguments.
	/// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string,string)"/> method,
	/// the <see cref="Join(IPath,string?,string?)"/> method does not attempt to root the returned path.
	/// (That is, if <paramref name="path2"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string,string)"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">A character span that contains the first path to join.</param>
	/// <param name="path2">A character span that contains the second path to join.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        if (path1.Length == 0)
            return path2.ToString();
        if (path2.Length == 0)
            return path1.ToString();
        return JoinInternal(path1, path2);
	}

	/// <summary>
	/// Concatenates three paths into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates <paramref name="path1"/>, <paramref name="path2"/> and <paramref name="path3"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the length of any of <paramref name="path1"/>, <paramref name="path2"/> and <paramref name="path3"/>
	/// argument is zero, the method concatenates the remaining arguments.
	/// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string,string,string)"/> method,
	/// the <see cref="Join(IPath,string?,string?,string?)"/> method does not attempt to root the returned path.
	/// (That is, if <paramref name="path2"/> or <paramref name="path3"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string,string,string)"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">The first path to join.</param>
	/// <param name="path2">The second path to join.</param>
	/// <param name="path3">The third path to join.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, string? path1, string? path2, string? path3)
    {
        if (string.IsNullOrEmpty(path1))
            return Join(_, path2, path3);
        if (string.IsNullOrEmpty(path2))
            return Join(_, path1, path3);
        if (string.IsNullOrEmpty(path3))
            return Join(_, path1, path2);
        return JoinInternal(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
	}

    /// <summary>
    /// Concatenates three path components into a single path.
    /// </summary>
    /// <remarks>
    /// This method simply concatenates <paramref name="path1"/>, <paramref name="path2"/> and <paramref name="path3"/>
    /// and adds a directory separator character between any of the path components if one is not already present.
    /// If the length of any of <paramref name="path1"/>, <paramref name="path2"/> and <paramref name="path3"/>
    /// argument is zero, the method concatenates the remaining arguments.
    /// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
    /// <br/>
    /// <br/>
    /// Unlike the <see cref="IPath.Combine(string,string,string)"/> method,
    /// the <see cref="Join(IPath,string?,string?,string?)"/> method does not attempt to root the returned path.
    /// (That is, if <paramref name="path2"/> or <paramref name="path3"/> is an absolute path,
    /// the <c>Join</c> method does not discard the previous paths as the Combine method does.)
    /// <br/>
    /// <br/>
    /// Not all invalid characters for directory and file names are interpreted as unacceptable by the
    /// <c>Join</c> method, because you can use these characters for search wildcard characters.
    /// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
    /// The <c>Join</c> method therefore successfully interprets it.
    /// </remarks>
    /// <param name="_">The file system's path instance.</param>
    /// <param name="path1">A character span that contains the first path to join.</param>
    /// <param name="path2">A character span that contains the second path to join.</param>
    /// <param name="path3">A character span that contains the third path to join.</param>
    /// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
    {
        if (path1.Length == 0)
            return Join(_, path2, path3);
        if (path2.Length == 0) 
            return Join(_, path1, path3);
        if (path3.Length == 0)
            return Join(_, path1, path2);
        return JoinInternal(path1, path2, path3);
	}

	/// <summary>
	/// Concatenates four paths into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates <paramref name="path1"/>, <paramref name="path2"/>, <paramref name="path3"/> and <paramref name="path4"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the length of any of <paramref name="path1"/>, <paramref name="path2"/>, <paramref name="path3"/> and <paramref name="path4"/>
	/// argument is zero, the method concatenates the remaining arguments.
	/// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string,string,string,string)"/> method,
	/// the <see cref="Join(IPath,string?,string?,string?,string?)"/> method does not attempt to root the returned path.
	/// (That is, if <paramref name="path2"/>, <paramref name="path3"/> or <paramref name="path4"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string,string,string,string)"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">The first path to join.</param>
	/// <param name="path2">The second path to join.</param>
	/// <param name="path3">The third path to join.</param>
	/// <param name="path4">The fourth path to join.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, string? path1, string? path2, string? path3, string? path4)
    {
        if (string.IsNullOrEmpty(path1))
            return Join(_, path2, path3, path4);
        if (string.IsNullOrEmpty(path2))
            return Join(_, path1, path3, path4);
        if (string.IsNullOrEmpty(path3))
            return Join(_, path1, path2, path4);
        if (string.IsNullOrEmpty(path4))
            return Join(_, path1, path2, path3);
        return JoinInternal(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
	}

	/// <summary>
	/// Concatenates four path components into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates <paramref name="path1"/>, <paramref name="path2"/>, <paramref name="path3"/> and <paramref name="path4"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the length of any of <paramref name="path1"/>, <paramref name="path2"/>, <paramref name="path3"/> and <paramref name="path4"/>
	/// argument is zero, the method concatenates the remaining arguments.
	/// If the length of the resulting concatenated string is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string,string,string,string)"/> method,
	/// the <see cref="Join(IPath,string?,string?,string?,string?)"/> method does not attempt to root the returned path.
	/// (That is, if <paramref name="path2"/>, <paramref name="path3"/> or <paramref name="path4"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string,string,string,string)"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">A character span that contains the first path to join.</param>
	/// <param name="path2">A character span that contains the second path to join.</param>
	/// <param name="path3">A character span that contains the third path to join.</param>
	/// <param name="path4">A character span that contains the fourth path to join.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
    {
        if (path1.Length == 0) 
            return Join(_, path2, path3, path4);
        if (path2.Length == 0) 
            return Join(_, path1, path3, path4);
        if (path3.Length == 0)
            return Join(_, path1, path2, path4);
        if (path4.Length == 0)
            return Join(_, path1, path2, path3);
        return JoinInternal(path1, path2, path3, path4);
	}

	/// <summary>
	/// Concatenates an array of paths into a single path.
	/// </summary>
	/// <remarks>
	/// This method simply concatenates all the strings in <paramref name="paths"/>
	/// and adds a directory separator character between any of the path components if one is not already present.
	/// If the <see cref="Array.Length"/> of any of the paths in <paramref name="paths"/> is zero,
	/// the method concatenates the remaining arguments.
	/// If the resulting concatenated string's length is zero, the method returns <see cref="string.Empty"/>.
	/// <br/>
	/// <br/>
	/// Unlike the <see cref="IPath.Combine(string[])"/> method,
	/// the <see cref="Join(IPath,string?[])"/> method does not attempt to root the returned path.
	/// (That is, if any of the paths in <paramref name="paths"/> is an absolute path,
	/// the <c>Join</c> method does not discard the previous paths as the <see cref="IPath.Combine(string[])"/> method does.)
	/// <br/>
	/// <br/>
	/// Not all invalid characters for directory and file names are interpreted as unacceptable by the
	/// <c>Join</c> method, because you can use these characters for search wildcard characters.
	/// For example, while <c>Path.Join("c:\\", "temp", "*.txt")</c> might be invalid when creating a file, it is valid as a search string.
	/// The <c>Join</c> method therefore successfully interprets it.
	/// </remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="paths">An array of paths.</param>
	/// <returns>The concatenated path.</returns>
	public static string Join(this IPath _, params string?[] paths)
    {
        ThrowHelper.ThrowIfNull(paths);
        return Join(_, (ReadOnlySpan<string?>)paths);
	}

	/// <summary>
	/// Attempts to concatenate two path components to a single preallocated character span,
	/// and returns a value that indicates whether the operation succeeded.
	/// </summary>
	/// <remarks><paramref name="destination"/> must be large enough to hold the concatenated path</remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">A character span that contains the first path to join.</param>
	/// <param name="path2">A character span that contains the second path to join.</param>
	/// <param name="destination">A character span to hold the concatenated path.</param>
	/// <param name="charsWritten">When the method returns, a value that indicates the number of characters written to the <paramref name="destination"/>.</param>
	/// <returns><see langword="true"/> if the concatenation operation is successful; otherwise, <see langword="false"/>.</returns>
	public static bool TryJoin(
		this IPath _,
        ReadOnlySpan<char> path1,
        ReadOnlySpan<char> path2,
        Span<char> destination,
        out int charsWritten)
    {
        charsWritten = 0;
        if (path1.Length == 0 && path2.Length == 0)
            return true;

        if (path1.Length == 0 || path2.Length == 0)
        {
            ref var pathToUse = ref path1.Length == 0 ? ref path2 : ref path1;
            if (destination.Length < pathToUse.Length)
                return false;

            pathToUse.CopyTo(destination);
            charsWritten = pathToUse.Length;
            return true;
        }

        var needsSeparator = !(HasTrailingDirectorySeparator(path1) || HasLeadingDirectorySeparator(path2));
        var charsNeeded = path1.Length + path2.Length + (needsSeparator ? 1 : 0);
        if (destination.Length < charsNeeded)
            return false;

        path1.CopyTo(destination);
        if (needsSeparator)
            destination[path1.Length] = DirectorySeparatorChar;

        path2.CopyTo(destination.Slice(path1.Length + (needsSeparator ? 1 : 0)));

        charsWritten = charsNeeded;
        return true;
	}

	/// <summary>
	/// Attempts to concatenate three path components to a single preallocated character span,
	/// and returns a value that indicates whether the operation succeeded.
	/// </summary>
	/// <remarks><paramref name="destination"/> must be large enough to hold the concatenated path</remarks>
	/// <param name="_">The file system's path instance.</param>
	/// <param name="path1">A character span that contains the first path to join.</param>
	/// <param name="path2">A character span that contains the second path to join.</param>
	/// <param name="path3">A character span that contains the third path to join.</param>
	/// <param name="destination">A character span to hold the concatenated path.</param>
	/// <param name="charsWritten">When the method returns, a value that indicates the number of characters written to the <paramref name="destination"/>.</param>
	/// <returns><see langword="true"/> if the concatenation operation is successful; otherwise, <see langword="false"/>.</returns>
	public static bool TryJoin(
		this IPath _,
		ReadOnlySpan<char> path1, 
        ReadOnlySpan<char> path2, 
        ReadOnlySpan<char> path3,
        Span<char> destination, 
        out int charsWritten)
    {
        charsWritten = 0;
        if (path1.Length == 0 && path2.Length == 0 && path3.Length == 0)
            return true;

        if (path1.Length == 0)
            return TryJoin(_, path2, path3, destination, out charsWritten);
        if (path2.Length == 0)
            return TryJoin(_, path1, path3, destination, out charsWritten);
        if (path3.Length == 0)
            return TryJoin(_, path1, path2, destination, out charsWritten);

        var neededSeparators = HasTrailingDirectorySeparator(path1) || HasLeadingDirectorySeparator(path2) ? 0 : 1;
        var needsSecondSeparator = !(HasTrailingDirectorySeparator(path2) || HasLeadingDirectorySeparator(path3));
        if (needsSecondSeparator)
            neededSeparators++;

        var charsNeeded = path1.Length + path2.Length + path3.Length + neededSeparators;
        if (destination.Length < charsNeeded)
            return false;

        var result = TryJoin(_, path1, path2, destination, out charsWritten);
        Debug.Assert(result, "should never fail joining first two paths");

        if (needsSecondSeparator)
            destination[charsWritten++] = DirectorySeparatorChar;

        path3.CopyTo(destination.Slice(charsWritten));
        charsWritten += path3.Length;

        return true;
	}

	private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
	{
		Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

		var hasSeparator = IsAnyDirectorySeparator(first[first.Length - 1]) || IsAnyDirectorySeparator(second[0]);

        var sb = new ValueStringBuilder(stackalloc char[260]);
		sb.Append(first);
		if (!hasSeparator)
			sb.Append(DirectorySeparatorChar);
		sb.Append(second);

		return sb.ToString();
	}

	private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
	{
		Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0, "should have dealt with empty paths");

        var firstHasSeparator = IsAnyDirectorySeparator(first[first.Length - 1]) || IsAnyDirectorySeparator(second[0]);
        var secondHasSeparator = IsAnyDirectorySeparator(second[second.Length - 1]) || IsAnyDirectorySeparator(third[0]);

        var sb = new ValueStringBuilder(stackalloc char[260]);

		sb.Append(first);
		if (!firstHasSeparator)
			sb.Append(DirectorySeparatorChar);
		sb.Append(second);
		if (!secondHasSeparator)
			sb.Append(DirectorySeparatorChar);
		sb.Append(third);

        return sb.ToString();
    }

	private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
	{
		Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0 && fourth.Length > 0, "should have dealt with empty paths");

        var hasSeparator1 = IsAnyDirectorySeparator(first[first.Length - 1]) || IsAnyDirectorySeparator(second[0]);
        var hasSeparator2 = IsAnyDirectorySeparator(second[second.Length - 1]) || IsAnyDirectorySeparator(third[0]);
        var hasSeparator3 = IsAnyDirectorySeparator(third[third.Length - 1]) || IsAnyDirectorySeparator(fourth[0]);

        var sb = new ValueStringBuilder(stackalloc char[260]);

        sb.Append(first);
        if (!hasSeparator1)
            sb.Append(DirectorySeparatorChar);
        sb.Append(second);
        if (!hasSeparator2)
            sb.Append(DirectorySeparatorChar);
        sb.Append(third);
		if (!hasSeparator3)
			sb.Append(DirectorySeparatorChar);
		sb.Append(fourth);

        return sb.ToString();
    }
}
#endif