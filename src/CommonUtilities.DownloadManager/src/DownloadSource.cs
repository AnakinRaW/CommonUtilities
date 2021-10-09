namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// The supported source file location of an <see cref="Engines.IDownloadEngine"/>.
/// </summary>
public enum DownloadSource
{
    /// <summary>
    /// The engine supports downloading files from the local file system or local network.
    /// </summary>
    File,
    /// <summary>
    /// The engine supports downloading files from the Internet.
    /// </summary>
    Internet,
}