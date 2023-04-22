namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Provides an <see cref="IDownloadManagerConfiguration"/>
/// </summary>
public interface IDownloadManagerConfigurationProvider
{
    /// <summary>
    /// Gets or creates the <see cref="IDownloadManagerConfiguration"/> 
    /// </summary>
    /// <returns>The configuration</returns>
    IDownloadManagerConfiguration GetConfiguration();
}