namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Service to query whether the current process is elevated.
/// </summary>
public interface IProcessElevation
{
    /// <summary>
    /// Returns <see langword="true"/> if the current process is elevated; <see langword="false"/> otherwise.
    /// </summary>
    bool IsCurrentProcessElevated { get; }
}