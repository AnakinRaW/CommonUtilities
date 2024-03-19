using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.AccessControl;
using System.Security.Principal;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Service for validating file and directory paths for the Windows Operating System.
/// </summary>
#if NET8_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public static class WindowsPathExtensions
{
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