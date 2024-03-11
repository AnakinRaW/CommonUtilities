namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Indicates the status of a file name validation.
/// </summary>
public enum FileNameValidationResult
{
    /// <summary>
    /// The file name is valid.
    /// </summary>
    Valid,
    /// <summary>
    /// The file name is either <see langword="null"/> or empty.
    /// </summary>
    NullOrEmpty,
    /// <summary>
    /// The file name contains an illegal character.
    /// </summary>
    InvalidCharacter,
    /// <summary>
    /// The file name is not valid, as it starts or ends with a white space (\u0020) character.
    /// </summary>
    /// <remarks>This result is Windows exclusive.</remarks>
    LeadingOrTrailingWhiteSpace,
    /// <summary>
    /// The file name is not valid, as it ends with a period ('.') character.
    /// </summary>
    /// <remarks>This result is Windows exclusive.</remarks>
    TrailingPeriod,
    /// <summary>
    /// The file name is reserved by the current system (e.g, on Windows such as 'CON') and thus cannot be used.
    /// </summary>
    SystemReserved
}