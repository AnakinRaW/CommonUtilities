namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Delegate representing a download progress report.
/// </summary>
/// <param name="status">The updated status to report.</param>
public delegate void DownloadUpdateCallback(DownloadUpdate status);