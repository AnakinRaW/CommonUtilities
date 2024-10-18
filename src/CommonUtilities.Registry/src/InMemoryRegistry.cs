using System;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Platform independent <see cref="IRegistry"/> implementation which stores keys in memory only.
/// </summary>
public sealed class InMemoryRegistry : IRegistry
{
    private readonly Dictionary<(RegistryView, RegistryHive), InMemoryRegistryKey> _rootKeys = new();

    private static readonly ICollection<RegistryView> RegistryViews =
        Enum.GetValues(typeof(RegistryView)).OfType<RegistryView>().ToList();

    /// <summary>
    /// Gets a value indicating whether sub key paths and key value names are case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; }

    private static readonly (RegistryHive, string)[] HivesAndNames =
    [
        (RegistryHive.None, "HKEY_NONE"),
        (RegistryHive.CurrentUser, "HKEY_CURRENT_USER"),
        (RegistryHive.LocalMachine, "HKEY_LOCAL_MACHINE"),
        (RegistryHive.ClassesRoot, "HKEY_CLASSES_ROOT")
    ];


    /// <summary>
    /// Creates a new instance of the <see cref="InMemoryRegistry"/> class with specified case-sensitivity.
    /// </summary>
    /// <param name="isCaseSensitive">Set the case-sensitivity of this registry view. Default value is <see langword="false"/>.</param>
    public InMemoryRegistry(bool isCaseSensitive = false)
    {
        IsCaseSensitive = isCaseSensitive;
        foreach (var (hive, name) in HivesAndNames)
            foreach (var view in RegistryViews)
                _rootKeys.Add((view, hive), new InMemoryRegistryKey(view, name, null, IsCaseSensitive));
    }

    /// <inheritdoc/>
    public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
    {
        var rootKey = _rootKeys.Where(kv => kv.Key.Item1 == view && kv.Key.Item2 == hive).Select(kvp => kvp.Value).FirstOrDefault();
        if (rootKey is null) 
            throw new InvalidOperationException($"Cannot find {view} root key for hive '{hive}'");
        return rootKey;
    }
}