namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Status of proxy resolution of a file download.
/// </summary>
public enum ProxyResolution
{
    /// <summary>
    /// No proxy resolution applied.
    /// </summary>
    Default,
    /// <summary>
    /// Used default credentials for proxy.
    /// </summary>
    DefaultCredentialsOrNoAutoProxy,
    /// <summary>
    /// Default system proxy with current credentials used..
    /// </summary>
    NetworkCredentials,
    /// <summary>
    /// No proxy used.
    /// </summary>
    DirectAccess,
    /// <summary>
    /// Faulted proxy resolution.
    /// </summary>
    Error,
}