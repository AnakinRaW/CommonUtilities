using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Sklavenwalker.CommonUtilities.FileSystem.Windows.NativeMethods;

namespace Sklavenwalker.CommonUtilities.FileSystem.Windows
{
    /// <summary>
    /// Service for validating file and directory paths for the Windows Operating System.
    /// </summary>
    public class WindowsPathService
    {
        private static readonly Regex RegexInvalidName =
            new("^(COM\\d|CLOCK\\$|LPT\\d|AUX|NUL|CON|PRN|(.*[\\ud800-\\udfff]+.*))$", RegexOptions.IgnoreCase);

        private static readonly Regex RegexNoRelativePathingName = new("^(.*\\.\\..*)$", RegexOptions.IgnoreCase);

        private static readonly Regex RegexInvalidPath =
            new("(^|\\\\)(AUX|CLOCK\\$|LPT|NUL|CON|PRN|COM\\d{1}|LPT\\d{1}|(.*[\\ud800-\\udfff]+.*))(\\\\|$)",
                RegexOptions.IgnoreCase);

        private static readonly Regex RegexSimplePath =
            new("^(([A-Z]:\\\\.*)|(\\\\\\\\.+\\\\.*))$", RegexOptions.IgnoreCase);

        private static readonly char[]
            InvalidNameChars = Path.GetInvalidFileNameChars().Concat("/?:&\\*#%;").ToArray();

        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        /// <summary>
        /// Creates a new service instance.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">If object is created on a non-Windows system.</exception>
        public WindowsPathService()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Only available on Windows");
        }

        /// <summary>
        /// Gets the <see cref="DriveType"/> of a given absolute path.
        /// </summary>
        /// <param name="location">The location the get the <see cref="DriveType"/> for.</param>
        /// <returns>The drive kind.</returns>
        /// <exception cref="InvalidOperationException">If the <paramref name="location"/> is not absolute.</exception>
        public DriveType GetDriveType(string location)
        {
            if (!Path.IsPathRooted(location))
                throw new InvalidOperationException("location not an absolute path.");
            string pathRoot = Path.GetPathRoot(location)!;
            if (pathRoot[pathRoot.Length - 1] != Path.DirectorySeparatorChar)
                pathRoot += Path.DirectorySeparatorChar.ToString();
            return (DriveType)Kernel32.GetDriveTypeW(pathRoot);
        }

        /// <summary>
        /// Checks if a given string is valid as an file name.
        /// <remarks><paramref name="fileName"/> must be without the desired file extension.</remarks>
        /// </summary>
        /// <param name="fileName">The candidate name to validate.</param>
        /// <returns><see langword="true"/> if the <paramref name="fileName"/> is valid; <see langword="false"/> otherwise.</returns>
        public bool IsValidFileName(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName) &&
                   !RegexInvalidName.IsMatch(fileName) &&
                   !RegexNoRelativePathingName.IsMatch(fileName) &&
                   fileName.IndexOfAny(InvalidNameChars) < 0;
        }

        /// <summary>
        /// Checks whether a given string is valid as an absolute directory path.
        /// <remarks>This implementation internally uses <see cref="Path.GetDirectoryName(string)"/>. So mind trailing slashes for the <paramref name="path"/>.</remarks>
        /// </summary>
        /// <param name="path">the candidate directory path to validate.</param>
        /// <returns><see langword="true"/> if the <paramref name="path"/> is valid; <see langword="false"/> otherwise.</returns>
        /// <exception cref="InvalidOperationException">If the <paramref name="path"/> is not absolute.</exception>
        public bool IsValidAbsolutePath(string path)
        {
            if (!Path.IsPathRooted(path))
                throw new InvalidOperationException("path not absolute.");
            return IsValidPath(path, true);
        }

        /// <summary>
        /// Checks whether a given string is valid as an directory path.
        /// <remarks>This implementation internally uses <see cref="Path.GetDirectoryName(string)"/>. So mind trailing slashes for the <paramref name="path"/>.</remarks>
        /// </summary>
        /// <param name="path">the candidate directory path to validate.</param>
        /// <returns><see langword="true"/> if the <paramref name="path"/> is valid; <see langword="false"/> otherwise.</returns>
        public bool IsValidPath(string path)
        {
            return IsValidPath(path, false);
        }

        // Based on: https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// <summary>
        /// Checks whether the current executing user that the requested rights on a given location.
        /// </summary>
        /// <param name="path">The directory to check rights on.</param>
        /// <param name="accessRights">The requested rights.</param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> does not exists.</exception>
        public bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            bool isInRoleWithAccess;
            var di = new DirectoryInfo(path);
            try
            {
                if (!di.Exists)
                    throw new DirectoryNotFoundException($"Unable to find {di.FullName}");
                isInRoleWithAccess = TestAccessRightsOnWindows(di, accessRights);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return isInRoleWithAccess;
        }
        
        private static bool IsValidPath(string path, bool forceAbsolute)
        {
            if (!IsPathValidAndNotEmpty(path, forceAbsolute))
                return false;
            try
            {
                path = Path.GetDirectoryName(path)!;
                if (!string.IsNullOrEmpty(path))
                {
                    if (RegexInvalidPath.IsMatch(path))
                        return false;
                    if (path.IndexOfAny(InvalidPathChars) >= 0)
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static bool IsPathValidAndNotEmpty(string path, bool checkAbsolute)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (checkAbsolute && !RegexSimplePath.IsMatch(path))
                return false;
            return path.IndexOfAny(InvalidPathChars) < 0;
        }

        private static bool TestAccessRightsOnWindows(DirectoryInfo directoryInfo, FileSystemRights accessRights)
        {
            var acl = directoryInfo.GetAccessControl();
            var rules = acl.GetAccessRules(true, true,
                // If Windows 7
                Environment.OSVersion.VersionString.StartsWith("6.1")
                    ? typeof(SecurityIdentifier)
                    : typeof(NTAccount));

            var currentUser = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentUser);
            foreach (AuthorizationRule rule in rules)
            {
                var fsAccessRule = rule as FileSystemAccessRule;
                if (fsAccessRule == null)
                    continue;

                if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                {
                    var ntAccount = rule.IdentityReference as NTAccount;
                    if (ntAccount == null)
                        continue;

                    if (principal.IsInRole(ntAccount.Value))
                    {
                        return fsAccessRule.AccessControlType != AccessControlType.Deny;
                    }
                }
            }

            return false;
        }
    }
}
