using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
#if NET9_0_OR_GREATER
using System.Threading;
#endif

namespace AnakinRaW.CommonUtilities.DownloadManager;

internal class LeastRecentlyUsedDownloadProviders
{
#if NET9_0_OR_GREATER
    private static readonly Lock SyncRoot = new();
#else
    private static readonly object SyncRoot = new();
#endif

    private readonly ConcurrentDictionary<string, int> _preferredProviders = new();

    public string? LastSuccessfulProvider
    {
        get;
        set
        {
            if (value == null)
                return;
            field = value;
            _preferredProviders.AddOrUpdate(value, 1, (_, existingVal) => ++existingVal);
        }
    }

    public IList<IDownloadProvider> GetProvidersInPriorityOrder(ICollection<IDownloadProvider> providers)
    { 
        lock (SyncRoot)
        { 
            var providerList = _preferredProviders
                .OrderByDescending(i => i.Value)
                .Select(providerNameAndFrequency => providers.FirstOrDefault(e =>
                    string.Equals(e.Name, providerNameAndFrequency.Key, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p != null)
                .ToList();
            foreach (var provider in providers)
            {
                if (!providerList.Contains(provider))
                    providerList.Add(provider);
            }
            return providerList!;
        }
    }
}