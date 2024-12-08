using System;

namespace AnakinRaW.CommonUtilities.FileSystem.Validation;

/// <summary>
/// The base class for a validator of file names.
/// </summary>
public abstract class FileNameValidator
{
    /// <summary>
    /// Checks whether a string represent a valid file name.
    /// </summary>
    /// <param name="fileName">The string to validate.</param>
    /// <returns>The result of the validation.</returns>
    public FileNameValidationResult IsValidFileName(string? fileName)
    {
        return fileName is null ? FileNameValidationResult.NullOrEmpty : IsValidFileName(fileName.AsSpan());
    }

    /// <summary>
    /// Checks whether a string represent a valid file name
    /// </summary>
    /// <param name="fileName">The string to validate.</param>
    public abstract FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName);
}