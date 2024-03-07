using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Service for validating file and directory paths for the Windows Operating System.
/// </summary>
public static class WindowsPathExtensions
{
    private static readonly Regex RegexInvalidName =
        new("^(COM\\d|CLOCK\\$|LPT\\d|AUX|NUL|CON|PRN|(.*[\\ud800-\\udfff]+.*))$", RegexOptions.IgnoreCase);

    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

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
            return whiteSpaceError
                ? FileNameValidationResult.LeadingOrTrailingWhiteSpace
                : FileNameValidationResult.TrailingPeriod;

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

    private static bool TestAccessRightsOnWindows(this IDirectoryInfo directoryInfo, FileSystemRights accessRights)
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
            if (rule is not FileSystemAccessRule fsAccessRule)
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