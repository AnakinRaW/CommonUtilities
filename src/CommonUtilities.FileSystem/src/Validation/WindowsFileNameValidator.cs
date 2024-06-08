using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AnakinRaW.CommonUtilities.FileSystem.Validation;

/// <summary>
/// A file name validator for the Windows.
/// </summary>
public class WindowsFileNameValidator : FileNameValidator
{
    /// <summary>
    /// Gets a singleton instance of the <see cref="WindowsFileNameValidator"/> class.
    /// </summary>
    public static readonly WindowsFileNameValidator Instance = new();
    
    private static readonly Regex RegexInvalidName =
        new("^(COM\\d|CLOCK\\$|LPT\\d|AUX|NUL|CON|PRN|(.*[\\ud800-\\udfff]+.*))$", RegexOptions.IgnoreCase);

    // From dotnet/runtime Path.Windows.cs
    private static char[] InvalidFileNameChars =>
    [
        '\"', '<', '>', '|',  ':', '*', '?', '\\', '/',
        //'\0', (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        //(char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        //(char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        //(char)31,
    ];

    /// <summary>
    /// Checks whether a string represent a valid file name
    /// </summary>
    /// <param name="fileName">The string to validate.</param>
    /// <param name="checkWindowsReservedNames">Determines whether the check shall include Windows reserved file names (e.g, AUX, LPT1, etc.).</param>
    /// <returns></returns>
    public FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName, bool checkWindowsReservedNames)
    {
        if (fileName.Length == 0)
            return FileNameValidationResult.NullOrEmpty;

        if (!EdgesValid(fileName, out var whiteSpaceError))
            return whiteSpaceError
                ? FileNameValidationResult.LeadingOrTrailingWhiteSpace
                : FileNameValidationResult.TrailingPeriod;

        if (ContainsInvalidChars(fileName))
            return FileNameValidationResult.InvalidCharacter;

        if (checkWindowsReservedNames)
        {
#if NET7_0_OR_GREATER
            if (RegexInvalidName.IsMatch(fileName))
                return FileNameValidationResult.SystemReserved;
#else
            if (RegexInvalidName.IsMatch(fileName.ToString()))
                return FileNameValidationResult.SystemReserved;
#endif

        }

        return FileNameValidationResult.Valid;
    }

    /// <inheritdoc />
    public override FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName)
    {
        return IsValidFileName(fileName, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsInvalidChars(ReadOnlySpan<char> value)
    {
        foreach (var t in value)
            if (IsInvalidFileCharacter(t))
                return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInvalidFileCharacter(char c)
    {
        if (c <= 31)
            return true;

        // Additional check for invalid Windows file name characters
        foreach (var charToCheck in InvalidFileNameChars)
        {
            if (charToCheck == c)
                return true;
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EdgesValid(ReadOnlySpan<char> value, out bool whiteSpace)
    {
        whiteSpace = false;

        if (value[0] is '\x0020')
        {
            whiteSpace = true;
            return false;
        }

#if NET
        var lastChar = value[^1];
#else
        var lastChar = value[value.Length - 1];
#endif
        if (lastChar is '\x0020')
        {
            whiteSpace = true;
            return false;
        }

        if (lastChar is '.')
            return false;

        return true;
    }
}