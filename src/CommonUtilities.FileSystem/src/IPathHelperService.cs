using System;
using System.IO;

namespace Sklavenwalker.CommonUtilities.FileSystem
{
    /// <summary>
    /// Path Utility service providing methods to work with IO paths.
    /// </summary>
    public interface IPathHelperService
    {
        /// <summary>
        /// Normalizes a given path according to given normalization rules.
        /// </summary>
        /// <param name="path">The input path.</param>
        /// <param name="options">The options how to normalize.</param>
        /// <returns>The normalized path.</returns>
        /// <exception cref="IOException">if the normalization failed. See the inner exception for details.</exception>
        string NormalizePath(string path, PathNormalizeOptions options);

        /// <summary>
        /// Returns <paramref name="pathToRelativize"/> relative to <paramref name="relativePathBase"/>, with default System Directory Separator character as separator.
        /// </summary>
        string GetRelativePath(string relativePathBase, string pathToRelativize);

        /// <summary>
        /// Ensures a trailing directory separator character.
        /// </summary>
        string EnsureTrailingSeparator(string path);

        /// <summary>
        /// Checks whether a candidate path is a child of a given base path.
        /// <remarks>No normalization and path resolving is performed beforehand.</remarks>
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="candidate">The sub path candidate.</param>
        /// <returns><see langword="true"/> if <paramref name="candidate"/> is a child of <paramref name="basePath"/>; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        bool IsChildOf(string basePath, string candidate);

        /// <summary>
        /// Checks whether a path is absolute.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns><see langword="true"/> if the input path is absolute; <see langword="false"/> otherwise.</returns>
        bool IsAbsolute(string path);
    }
}