using Microsoft.Extensions.DependencyInjection;
using Sklavenwalker.CommonUtilities.DownloadManager.Verification;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Contains initialization routines for this library.
/// </summary>
public static class DownloadManagerLibrary
{
    /// <summary>
    /// Initializes this library with a given <see cref="IServiceCollection"/>
    /// </summary>
    public static void InitializeLibrary(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IVerifier>(sp => new HashVerifier(sp));
    }
}