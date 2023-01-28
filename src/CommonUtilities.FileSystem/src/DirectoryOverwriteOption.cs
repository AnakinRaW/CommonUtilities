using System.IO;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Specifies how moving a directory shall behave.
/// </summary>
public enum DirectoryOverwriteOption
{
    /// <summary>
    /// If the target directory already exists, an <see cref="IOException"/> will be thrown.
    /// </summary>
    NoOverwrite,
    /// <summary>
    /// If the target directory already exists, all files from source will be written into the existing directory
    /// and get overwritten if already present.
    /// </summary>
    MergeOverwrite,
    /// <summary>
    /// If the target directory already exists, it gets removed before moving the source directory.
    /// </summary>
    CleanOverwrite
}