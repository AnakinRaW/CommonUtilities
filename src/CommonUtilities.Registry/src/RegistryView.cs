namespace AnakinRaW.CommonUtilities.Registry;

/// <summary>
/// Specifies which registry view to target on a 64-bit operating system.
/// </summary>
public enum RegistryView
{
    /// <summary>
    /// The default view.
    /// </summary>
    Default,
    /// <summary>
    /// The 32-bit view.
    /// </summary>
    Registry32,
    /// <summary>
    /// The 64-bit view.
    /// </summary>
    Registry64,
    /// <summary>
    /// The default view to the current operating system.
    /// </summary>
    DefaultOperatingSystem,
}