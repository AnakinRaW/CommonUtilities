using System;
using System.IO;
using System.Security.AccessControl;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Service for validating file and directory paths for the Windows Operating System.
/// </summary>
public interface IWindowsPathService
{
    /// <summary>
    /// Gets the <see cref="DriveType"/> of a given absolute path.
    /// </summary>
    /// <param name="location">The location the get the <see cref="DriveType"/> for.</param>
    /// <returns>The drive kind.</returns>
    /// <exception cref="InvalidOperationException">If the <paramref name="location"/> is not absolute.</exception>
    DriveType GetDriveType(string location);

    /// <summary>
    /// Checks if a given string is valid as an file name.
    /// <remarks><paramref name="fileName"/> must be without the desired file extension.</remarks>
    /// </summary>
    /// <param name="fileName">The candidate name to validate.</param>
    /// <returns><see langword="true"/> if the <paramref name="fileName"/> is valid; <see langword="false"/> otherwise.</returns>
    bool IsValidFileName(string fileName);

    /// <summary>
    /// Checks whether a given string is valid as an absolute directory path.
    /// <remarks>This implementation internally uses <see cref="Path.GetDirectoryName(string)"/>. So mind trailing slashes for the <paramref name="path"/>.</remarks>
    /// </summary>
    /// <param name="path">the candidate directory path to validate.</param>
    /// <returns><see langword="true"/> if the <paramref name="path"/> is valid; <see langword="false"/> otherwise.</returns>
    /// <exception cref="InvalidOperationException">If the <paramref name="path"/> is not absolute.</exception>
    bool IsValidAbsolutePath(string path);

    /// <summary>
    /// Checks whether a given string is valid as an directory path.
    /// <remarks>This implementation internally uses <see cref="Path.GetDirectoryName(string)"/>. So mind trailing slashes for the <paramref name="path"/>.</remarks>
    /// </summary>
    /// <param name="path">the candidate directory path to validate.</param>
    /// <returns><see langword="true"/> if the <paramref name="path"/> is valid; <see langword="false"/> otherwise.</returns>
    bool IsValidPath(string path);

    /// <summary>
    /// Checks whether the current executing user that the requested rights on a given location.
    /// </summary>
    /// <param name="path">The directory to check rights on.</param>
    /// <param name="accessRights">The requested rights.</param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> does not exists.</exception>
    bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights);
}