using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

internal class PreferredDownloadProviders
{
    private static readonly object SyncRoot = new();
    private readonly ConcurrentDictionary<string, int> _preferredProviders;
    private string? _lastSuccessfulProviderName;

    public string? LastSuccessfulProviderName
    {
        get => _lastSuccessfulProviderName;
        set
        {
            if (value == null)
                return;
            _lastSuccessfulProviderName = value;
            _preferredProviders.AddOrUpdate(value, 1, (_, existingVal) =>
            {
                ++existingVal;
                return existingVal;
            });
        }
    }

    public PreferredDownloadProviders()
    {
        _preferredProviders = new ConcurrentDictionary<string, int>();
    }

    public IEnumerable<IDownloadProvider> GetProvidersInPriorityOrder(IEnumerable<IDownloadProvider> providers)
    { 
        lock (SyncRoot)
        {
            var providerList = _preferredProviders
                .OrderByDescending(i => i.Value)
                .Select(providerNameAndFrequency => providers.FirstOrDefault(e =>
                    string.Equals(e.Name, providerNameAndFrequency.Key, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p != null).ToList();
            foreach (var provider in providers)
            {
                if (!providerList.Contains(provider))
                    providerList.Add(provider);
            }
            return providerList!;
        }
    }
}