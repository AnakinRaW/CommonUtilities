namespace Sklavenwalker.CommonUtilities.Registry;

/// <summary>
/// Represents the possible values for a top-level node.
/// </summary>
public enum RegistryHive
{
    /// <summary>
    /// Represents NO base key.
    /// </summary>
    None,
    /// <summary>
    /// Represents the HKEY_CLASSES_ROOT base key.
    /// </summary>
    ClassesRoot,
    /// <summary>
    /// Represents the HKEY_LOCAL_MACHINE base key.
    /// </summary>
    LocalMachine,
    /// <summary>
    /// Represents the HKEY_CURRENT_USER base key.
    /// </summary>
    CurrentUser,
}