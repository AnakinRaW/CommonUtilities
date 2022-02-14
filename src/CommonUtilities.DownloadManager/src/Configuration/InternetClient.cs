namespace Sklavenwalker.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Specifies the available implementations for Internet-based downloads
/// </summary>
public enum InternetClient
{
    /// <summary>
    /// Uses the <see cref="System.Net.Http.HttpClient"/> API
    /// </summary>
    HttpClient,
    /// <summary>
    /// Uses the <see cref="System.Net.WebClient"/> API
    /// </summary>
    WebClient
}