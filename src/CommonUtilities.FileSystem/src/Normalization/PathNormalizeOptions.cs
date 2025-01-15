namespace AnakinRaW.CommonUtilities.FileSystem.Normalization;

/// <summary>
/// Options how path normalization shall be performed.
/// </summary>
/// <remarks>
/// The <see cref="PathNormalizeOptions"/> structure includes some static properties that return predefined options.
/// </remarks>
public record struct PathNormalizeOptions
{
    /// <summary>
    /// Returns a <see cref="PathNormalizeOptions"/> to normalize paths using the current system's primary directory separators.
    /// </summary>
    public static readonly PathNormalizeOptions UnifySeparators = new()
    {
        UnifyDirectorySeparators = true,
        UnifySeparatorKind = DirectorySeparatorKind.System
    };

    /// <summary>
    /// Returns a <see cref="PathNormalizeOptions"/> to remove any trailing directory separators.
    /// </summary>
    public static readonly PathNormalizeOptions TrimTrailingSeparators = new()
    {
        TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Trim
    };

    /// <summary>
    /// Returns a <see cref="PathNormalizeOptions"/> to ensure there is any trailing directory separators.
    /// </summary>
    public static readonly PathNormalizeOptions EnsureTrailingSeparator = new()
    {
        TrailingDirectorySeparatorBehavior = TrailingDirectorySeparatorBehavior.Ensure
    };

    /// <summary>
    /// Returns a <see cref="PathNormalizeOptions"/> to normalize paths on case-insensitive file systems using upper case based on the invariant culture.
    /// </summary>
    public static readonly PathNormalizeOptions UnifyUpper = new()
    {
        UnifyCase = UnifyCasingKind.UpperCase
    };

    /// <summary>
    /// Returns a <see cref="PathNormalizeOptions"/> to normalize paths using upper case based on the invariant culture,
    /// even if the current file system is case-sensitive.
    /// </summary>
    public static readonly PathNormalizeOptions AlwaysUnifyUpper = new()
    {
        UnifyCase = UnifyCasingKind.UpperCaseForce
    };

    /// <summary>
    /// Gets or sets whether directory separator shall be unified across the path based on <see cref="UnifySeparatorKind"/>.
    /// </summary>
    public bool UnifyDirectorySeparators { get; init; }

    /// <summary>
    /// Gets or sets whether directory separator normalization on a Linux system shall treat backslash characters '\'
    /// as a separator and normalize it. When set to <see langword="true"/>, a backslash character is treated as a directory separator.
    /// When set to <see langword="false"/>, a backslash character is not treated as a directory separator and thus is not getting normalized.
    /// </summary>
    public bool TreatBackslashAsSeparator { get; init; }

    /// <summary>
    /// Gets or sets a value how directory separators shall be treated. Default is <see cref="DirectorySeparatorKind.System"/>.
    /// </summary>
    public DirectorySeparatorKind UnifySeparatorKind { get; init; }

    /// <summary>
    /// Gets or sets a value which casing option to apply. Default is <see cref="UnifyCasingKind.None"/>.
    /// </summary>
    public UnifyCasingKind UnifyCase { get; init; }

    /// <summary>
    /// Gets or sets a value how trailing directory separators handled.
    /// </summary>
    public TrailingDirectorySeparatorBehavior TrailingDirectorySeparatorBehavior { get; init; }
}