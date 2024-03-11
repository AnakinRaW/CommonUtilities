using System;

namespace AnakinRaW.CommonUtilities.FileSystem.Windows;

/// <summary>
/// Allows validation of file names.
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
        if (fileName is null)
            return FileNameValidationResult.NullOrEmpty;
        return IsValidFileName(fileName.AsSpan());
    }

    /// <summary>
    /// Checks whether a string represent a valid file name
    /// </summary>
    /// <param name="fileName">The string to validate.</param>
    public abstract FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName);
}