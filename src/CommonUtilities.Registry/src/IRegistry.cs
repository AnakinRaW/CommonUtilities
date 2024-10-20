namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Provides <see cref="IRegistryKey"/> objects that represent the root keys in the based on the Windows registry layout.
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Gets a value indicating whether sub key paths and key value names are case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; }

    /// <summary>
    /// Opens a new <see cref="IRegistryKey"/> that represents the requested key on the local machine
    /// with the specified view.
    /// </summary>
    /// <param name="hive">The HKEY to open.</param>
    /// <param name="view">The registry view to use.</param>
    /// <returns></returns>
    IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view);
}