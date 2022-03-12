using Sklavenwalker.CommonUtilities.DownloadManager.Providers;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// The supported source file location of an <see cref="IDownloadProvider"/>.
/// </summary>
public enum DownloadSource
{
    /// <summary>
    /// The provider supports downloading files from the local file system or local network.
    /// </summary>
    File,
    /// <summary>
    /// The provider supports downloading files from the Internet.
    /// </summary>
    Internet,
}