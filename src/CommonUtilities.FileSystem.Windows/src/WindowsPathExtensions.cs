using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Vanara.PInvoke;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Service for validating file and directory paths for the Windows Operating System.
/// </summary>
public static class WindowsPathExtensions
{
    private static readonly Regex RegexInvalidName =
        new("^(COM\\d|CLOCK\\$|LPT\\d|AUX|NUL|CON|PRN|(.*[\\ud800-\\udfff]+.*))$", RegexOptions.IgnoreCase);

    private static readonly Regex RegexNoRelativePathingName = new("^(.*\\.\\..*)$", RegexOptions.IgnoreCase);

    private static readonly Regex RegexInvalidPath =
        new("(^|\\\\)(AUX|CLOCK\\$|LPT|NUL|CON|PRN|COM\\d{1}|LPT\\d{1}|(.*[\\ud800-\\udfff]+.*))(\\\\|$)",
            RegexOptions.IgnoreCase);

    private static readonly Regex RegexSimplePath =
        new("^(([A-Z]:\\\\.*)|(\\\\\\\\.+\\\\.*))$", RegexOptions.IgnoreCase);

    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// Gets the <see cref="DriveType"/> of a given absolute path.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="location">The location the get the <see cref="DriveType"/> for.</param>
    /// <returns>The drive kind.</returns>
    /// <exception cref="PlatformNotSupportedException">If the current system is not Windows.</exception>
    public static DriveType GetDriveType(this IPath _, string location)
    {
        ThrowHelper.ThrowIfNotWindows();
        location = _.GetFullPath(location);
        var pathRoot = _.GetPathRoot(location)!;
        if (pathRoot[pathRoot.Length - 1] != _.DirectorySeparatorChar)
            pathRoot += _.DirectorySeparatorChar.ToString();
        return (DriveType)Kernel32.GetDriveType(pathRoot);
    }

    /// <summary>
    /// Checks whether a file name is valid on Windows.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="fileName">The file name to validate.</param>
    /// <returns><see langword="true"/> if the file name is valid; Otherwise, <see langword="false"/>.</returns>
    /// <exception cref="PlatformNotSupportedException">If the current system is not Windows.</exception>
    public static FileNameValidationResult IsValidFileName(this IPath _, string fileName)
    {
        ThrowHelper.ThrowIfNotWindows();

        if (string.IsNullOrEmpty(fileName))
            return FileNameValidationResult.NullOrEmpty;

        var fileNameSpan = fileName.AsSpan();

        if (!EdgesValid(fileNameSpan, out var whiteSpaceError))
            return whiteSpaceError ? FileNameValidationResult.LeadingOrTrailingWhiteSpace : FileNameValidationResult.TrailingPeriod;

        if (ContainsInvalidChars(fileNameSpan))
            return FileNameValidationResult.InvalidCharacter;

        if (RegexInvalidName.IsMatch(fileName))
            return FileNameValidationResult.WindowsReserved;

        return FileNameValidationResult.Valid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsInvalidChars(ReadOnlySpan<char> value)
    {
        foreach (var t in value)
            if (IsInvalidFileCharacter(t))
                return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInvalidFileCharacter(char c)
    { 
        // Additional check for invalid Windows file name characters
        foreach (var charToCheck in InvalidNameChars)
        {
            if (charToCheck == c)
                return true;
        }
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EdgesValid(ReadOnlySpan<char> value, out bool whiteSpace)
    {
        whiteSpace = false;

        if (value[0] is '\x0020')
        {
            whiteSpace = true;
            return false;
        }

#if NET
        var lastChar = value[^1];
#else
        var lastChar = value[value.Length - 1];
#endif
        if (lastChar is '\x0020')
        {
            whiteSpace = true;
            return false;
        }

        if (lastChar is '.')
            return false;

        return true;
    }



    /// <summary>
    /// Checks whether a given string is valid as an directory path.
    /// <remarks>This implementation internally uses <see cref="Path.GetDirectoryName(string)"/>. So mind trailing slashes for the <paramref name="path"/>.</remarks>
    /// </summary>
    /// <param name="_"></param>
    /// <param name="path">the candidate directory path to validate.</param>
    /// <returns><see langword="true"/> if the <paramref name="path"/> is valid; <see langword="false"/> otherwise.</returns>
    /// <exception cref="PlatformNotSupportedException">If the current system is not Windows.</exception>
    public static bool IsValidPath(this IPath _, string path)
    {
        ThrowHelper.ThrowIfNotWindows();
        return IsValidPath(_, path, false);
    }

    // Based on: https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
    /// <summary>
    /// Checks whether the current executing user that the requested rights on a given location.
    /// </summary>
    /// <param name="directoryInfo">The directory to check rights on.</param>
    /// <param name="accessRights">The requested rights.</param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException">If <paramref name="directoryInfo"/> does not exists.</exception>
    /// <exception cref="PlatformNotSupportedException">If the current system is not Windows.</exception>
    public static bool UserHasDirectoryAccessRights(this IDirectoryInfo directoryInfo, FileSystemRights accessRights)
    {
        ThrowHelper.ThrowIfNotWindows();
        bool isInRoleWithAccess;
        try
        {
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"Unable to find {directoryInfo.FullName}");
            isInRoleWithAccess = TestAccessRightsOnWindows(directoryInfo, accessRights);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return isInRoleWithAccess;
    }
        
    private static bool IsValidPath(IPath fsPath, string path, bool forceAbsolute)
    {
        if (!IsPathValidAndNotEmpty(path, forceAbsolute))
            return false;
        try
        {
            path = fsPath.GetDirectoryName(path)!;
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

    private static bool TestAccessRightsOnWindows(IDirectoryInfo directoryInfo, FileSystemRights accessRights)
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

/// <summary>
/// Indicates the status of a file name validation.
/// </summary>
public enum FileNameValidationResult
{
    /// <summary>
    /// The file name is valid.
    /// </summary>
    Valid,
    /// <summary>
    /// The file name is either <see langword="null"/> or empty.
    /// </summary>
    NullOrEmpty,
    /// <summary>
    /// The file name contains an illegal character.
    /// </summary>
    InvalidCharacter,
    /// <summary>
    /// The file name starts or ends with a white space (\u0020) character.
    /// </summary>
    LeadingOrTrailingWhiteSpace,
    /// <summary>
    /// The file name ends with a period ('.') character.
    /// </summary>
    TrailingPeriod,
    /// <summary>
    /// The file name is reserved by windows (such as 'CON') and thus cannot be used.
    /// </summary>
    WindowsReserved
}