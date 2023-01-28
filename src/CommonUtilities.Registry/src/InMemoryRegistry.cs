using System;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Platform independent <see cref="IRegistry"/> implementation which stores keys in memory only.
/// </summary>
public class InMemoryRegistry : IRegistry
{
    private readonly Dictionary<(RegistryView, RegistryHive), InMemoryRegistryKey> _rootKeys = new();

    /// <summary>
    /// Creates a new registry instance.
    /// </summary>
    public InMemoryRegistry()
    {
        var hivesAndNames = new[]
        {
            (RegistryHive.None, string.Empty),
            (RegistryHive.CurrentUser, "HKEY_CURRENT_USER"),
            (RegistryHive.LocalMachine, "HKEY_LOCAL_MACHINE"),
            (RegistryHive.ClassesRoot, "HKEY_CLASSES_ROOT"),
        };
        foreach (var (hive, name) in hivesAndNames)
        {
            foreach (var view in Enum.GetValues(typeof(RegistryView)).OfType<RegistryView>())
            {
                _rootKeys.Add((view, hive), new InMemoryRegistryKey(view, name));
            }
        }
    }

    /// <inheritdoc/>
    public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
    {
        var rootKey = _rootKeys.Where(kv => kv.Key.Item1 == view && kv.Key.Item2 == hive).Select(kvp => kvp.Value).FirstOrDefault();
        if (rootKey == null) throw new InvalidOperationException($"Cannot find {view} root key for hive '{hive}'");
        return rootKey;
    }
}