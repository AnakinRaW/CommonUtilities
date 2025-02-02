using System;
using System.Runtime.CompilerServices;

namespace AnakinRaW.CommonUtilities.FileSystem.Validation;

/// <summary>
/// A file name validator for the Linux-based systems.
/// </summary>
public sealed class LinuxFileNameValidator : FileNameValidator
{
    /// <summary>
    /// Gets a singleton instance of the <see cref="LinuxFileNameValidator"/> class.
    /// </summary>
    public static readonly LinuxFileNameValidator Instance = new();

    private LinuxFileNameValidator()
    {
    }

    /// <inheritdoc />
    public override FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName)
    {
        if (fileName.Length == 0)
            return FileNameValidationResult.NullOrEmpty;

        // Do not allow "."
        if (fileName.Length == 1 && fileName[0] == '.')
            return FileNameValidationResult.SystemReserved;

        // Do not allow ".."
        if (fileName.Length == 2 && fileName[0] == '.' && fileName[1] == '.')
            return FileNameValidationResult.SystemReserved;

        if (ContainsInvalidChars(fileName))
            return FileNameValidationResult.InvalidCharacter;

        return FileNameValidationResult.Valid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsInvalidChars(ReadOnlySpan<char> value)
    {
        // From dotnet/runtime Path.Unix.cs
        return value.IndexOfAny('\0', '/') != -1;
    }
}