using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.DownloadManager.Configuration;

/// <summary>
/// Base class that creates an <see cref="IDownloadManagerConfiguration"/> once and reuses this configuration.
/// </summary>
public abstract class DownloadManagerConfigurationProviderBase : IDownloadManagerConfigurationProvider
{
    private IDownloadManagerConfiguration _configuration = null!;

    /// <inheritdoc/>
    public IDownloadManagerConfiguration GetConfiguration()
    {
        var configuration = LazyInitializer.EnsureInitialized(ref _configuration, CreateConfiguration);
        if (configuration is null)
            throw new InvalidOperationException();
        return configuration;
    }

    /// <summary>
    /// Creates a new <see cref="IDownloadManagerConfiguration"/> instance.
    /// </summary>
    /// <returns>The created configuration</returns>
    protected abstract IDownloadManagerConfiguration CreateConfiguration();
}