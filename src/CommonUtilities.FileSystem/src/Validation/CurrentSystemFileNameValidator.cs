﻿using System;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.FileSystem.Validation;

/// <summary>
/// A file name validator for the current platform.
/// </summary>
public class CurrentSystemFileNameValidator : FileNameValidator
{
    /// <summary>
    /// Returns a singleton instance of the <see cref="CurrentSystemFileNameValidator"/> class.
    /// </summary>
    public static readonly CurrentSystemFileNameValidator Instance = new();

    /// <inheritdoc />
    public override FileNameValidationResult IsValidFileName(ReadOnlySpan<char> fileName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsFileNameValidator.Instance.IsValidFileName(fileName)
            : LinuxFileNameValidator.Instance.IsValidFileName(fileName);
    }
}