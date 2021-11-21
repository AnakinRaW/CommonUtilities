using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sklavenwalker.CommonUtilities.FileSystem
{
    // Based on https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/FileSystem/PathUtilities.cs
    // and https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Common/PathUtil/PathUtility.cs
    /// <inheritdoc cref="IPathHelperService"/>
    public class PathHelperService : IPathHelperService
    {
        private static readonly bool IsUnixLikePlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly char DirectorySeparatorChar = IsUnixLikePlatform ? '/' : '\\';
        private static readonly Lazy<bool> IsFileSystemCaseInsensitive = new(CheckIfFileSystemIsCaseInsensitive);
        private static readonly char[] Slashes = { '/', '\\' };
        internal const char VolumeSeparatorChar = ':';

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Creates a new instance with the system native <see cref="IFileSystem"/> implementation.
        /// </summary>
        public PathHelperService() : this(new System.IO.Abstractions.FileSystem())
        {
        }


        /// <summary>
        /// Creates a new instance with a given <see cref="IFileSystem"/>
        /// </summary>
        /// <param name="fileSystem">The file system to use for this instance.</param>
        public PathHelperService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public string NormalizePath(string path, PathNormalizeOptions options)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            // Needs to be the first operation!
            if (options.HasFlag(PathNormalizeOptions.UnifySlashes))
                path = GetPathWithDirectorySeparator(path);

            if (options.HasFlag(PathNormalizeOptions.ResolveFullPath))
                path = _fileSystem.Path.GetFullPath(path);

            if (options.HasFlag(PathNormalizeOptions.RemoveAdjacentSlashes))
            {
                // No need to do this again.
                if (!options.HasFlag(PathNormalizeOptions.ResolveFullPath))
                    path = RemoveAdjacentChars(path, 1);
            }
            
            if (options.HasFlag(PathNormalizeOptions.TrimTrailingSeparator)) 
                path = TrimTrailingSeparators(path);

            if (options.HasFlag(PathNormalizeOptions.ToLowerCase) && IsFileSystemCaseInsensitive.Value)
                path = path.ToLower();

            return path;
        }

        private static string RemoveAdjacentChars(string value, int startIndex)
        {
            if (startIndex >= value.Length)
                return value;
            StringBuilder stringBuilder = new(value);
            var lastChar = char.MinValue;
            for (var index = startIndex; index < stringBuilder.Length; ++index)
            {
                var currentChar = stringBuilder[index];
                var currentIsAdjacent = currentChar == lastChar && Slashes.Contains(currentChar);
                lastChar = currentChar;
                if (currentIsAdjacent)
                {
                    stringBuilder.Remove(index, 1);
                    --index;
                }
            }
            return stringBuilder.ToString();
        }

        private static string TrimTrailingSeparators(string s)
        {
            var lastSeparator = s.Length;
            while (lastSeparator > 0 && IsDirectorySeparator(s[lastSeparator - 1]))
                lastSeparator -= 1;
            if (lastSeparator != s.Length)
                s = s.Substring(0, lastSeparator);
            return s;
        }

        private static string GetPathWithDirectorySeparator(string path)
        {
            return IsUnixLikePlatform ? GetPathWithForwardSlashes(path) : GetPathWithBackSlashes(path);
        }

        private static string GetPathWithBackSlashes(string path)
        {
            return path.Replace('/', '\\');
        }

        private static string GetPathWithForwardSlashes(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <inheritdoc/>
        public string GetRelativePath(string relativePathBase, string pathToRelativize)
        {
            if (relativePathBase is null) throw new ArgumentNullException(nameof(relativePathBase));
            if (pathToRelativize is null) throw new ArgumentNullException(nameof(pathToRelativize));
            return GetRelativePath(relativePathBase, pathToRelativize, DirectorySeparatorChar);
        }

        private string GetRelativePath(string relativePathBase, string pathToRelativize, char separator)
        {
            StringComparison compare;
            if (!IsUnixLikePlatform)
            {
                compare = StringComparison.OrdinalIgnoreCase;
                // check if paths are on the same volume
                if (!string.Equals(_fileSystem.Path.GetPathRoot(relativePathBase), _fileSystem.Path.GetPathRoot(pathToRelativize), compare))
                {
                    // on different volumes, "relative" path is just path2
                    return pathToRelativize;
                }
            }
            else
                compare = StringComparison.Ordinal;

            var index = 0;
            var baseSegments = relativePathBase.Split(Slashes);
            var relativizeSegments = pathToRelativize.Split(Slashes);
            // if basePath does not end with / it is assumed the end is not a directory
            // we will assume that is isn't a directory by ignoring the last split
            var len1 = baseSegments.Length - 1;
            var len2 = relativizeSegments.Length;

            // find largest common absolute path between both paths
            var min = Math.Min(len1, len2);
            while (min > index)
            {
                if (!string.Equals(baseSegments[index], relativizeSegments[index], compare))
                    break;
                // Handle scenarios where folder and file have same name (only if os supports same name for file and directory)
                // e.g. /file/name /file/name/app
                if (len1 == index && len2 > index + 1 || len1 > index && len2 == index + 1)
                    break;
                ++index;
            }

            var path = "";

            // check if path2 ends with a non-directory separator and if path1 has the same non-directory at the end
            if (len1 + 1 == len2 && !string.IsNullOrEmpty(baseSegments[index]) &&
                string.Equals(baseSegments[index], relativizeSegments[index], compare))
                return path;

            for (var i = index; len1 > i; ++i) 
                path += ".." + separator;
            for (var i = index; len2 - 1 > i; ++i) 
                path += relativizeSegments[i] + separator;
            // if path2 doesn't end with an empty string it means it ended with a non-directory name, so we add it back
            if (!string.IsNullOrEmpty(relativizeSegments[len2 - 1])) 
                path += relativizeSegments[len2 - 1];

            return path;
        }

        /// <inheritdoc/>
        public string EnsureTrailingSeparator(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0 || IsDirectorySeparator(path[path.Length - 1]))
                return path;

            // Use the existing slashes in the path, if they're consistent
            var hasSlash = path.IndexOf('/') >= 0;
            var hasBackslash = path.IndexOf('\\') >= 0;
            return hasSlash switch
            {
                true when !hasBackslash => path + '/',
                false when hasBackslash => path + '\\',
                _ => path + DirectorySeparatorChar
            };
        }

        /// <inheritdoc/>
        public bool IsChildOf(string basePath, string candidate)
        {
            if (basePath is null) 
                throw new ArgumentNullException(nameof(basePath));
            if (candidate is null) 
                throw new ArgumentNullException(nameof(candidate));
            var comparison = IsFileSystemCaseInsensitive.Value
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return candidate.StartsWith(basePath, comparison);
        }

        /// <inheritdoc/>
        public bool IsAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (IsUnixLikePlatform)
                return path[0] == '/';

            if (IsDriveRootedAbsolutePath(path))
                return true;
            return path.Length >= 2 &&
                   IsDirectorySeparator(path[0]) &&
                   IsDirectorySeparator(path[1]);
        }

        private static bool IsDirectorySeparator(char c)
        {
            return Array.IndexOf(Slashes, c) >= 0;
        }

        private static bool IsDriveRootedAbsolutePath(string path)
        {
            return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
        }

        private static StringComparer GetStringComparerBasedOnOS()
        {
            return IsFileSystemCaseInsensitive.Value ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

#pragma warning disable IO0006
#pragma warning disable IO0003
        private static bool CheckIfFileSystemIsCaseInsensitive()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;
            var listOfPathsToCheck = new[]
            {

                Path.GetTempPath(),
                Directory.GetCurrentDirectory()
            };

            var isCaseInsensitive = true;
            foreach (var path in listOfPathsToCheck)
            {
                var result = CheckCaseSensitivityRecursivelyTillDirectoryExists(path, out var ignore);
                if (!ignore) 
                    isCaseInsensitive &= result;
            }
            return isCaseInsensitive;
        }

        private static bool CheckCaseSensitivityRecursivelyTillDirectoryExists(string path, out bool ignoreResult)
        {
            path = Path.GetFullPath(path);
            ignoreResult = true;
            var parentDirectoryFound = true;
            while (true)
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                if (path.Length <= 1)
                {
                    ignoreResult = true;
                    parentDirectoryFound = false;
                    break;
                }
                if (Directory.Exists(path))
                {
                    ignoreResult = false;
                    break;
                }
                path = Path.GetDirectoryName(path)!;
            }

            if (parentDirectoryFound)
            {
                return Directory.Exists(path.ToLowerInvariant()) && Directory.Exists(path.ToUpperInvariant());
            }
            return false;
        }
#pragma warning restore IO0006
#pragma warning restore IO0003
    }
}
