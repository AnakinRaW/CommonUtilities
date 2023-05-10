namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Service to query whether the current process is elevated.
/// </summary>
public interface ICurrentProcessInfo
{
    /// <summary>
    /// Returns <see langword="true"/> if the current process is elevated; <see langword="false"/> otherwise.
    /// </summary>
    bool IsElevated { get; }

    /// <summary>
    /// Gets the file path of the current process, or null if the path could not be determined.
    /// </summary>
    string? ProcessFilePath { get; }

    /// <summary>
    /// Gets the ID of the current process.
    /// </summary>
    int Id { get; }
}