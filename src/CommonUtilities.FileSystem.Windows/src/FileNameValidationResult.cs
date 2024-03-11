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
    /// The file name starts or ends with a white space (\u0020) character.
    /// </summary>
    LeadingOrTrailingWhiteSpace,
    /// <summary>
    /// The file name ends with a period ('.') character.
    /// </summary>
    TrailingPeriod,
    /// <summary>
    /// The file name is reserved by windows (such as 'CON') and thus cannot be used.
    /// </summary>
    WindowsReserved
}