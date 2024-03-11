namespace AnakinRaW.CommonUtilities.FileSystem.Normalization;

/// <summary>
/// Determines how path normalization shall handle trailing separators
/// </summary>
public enum TrailingDirectorySeparatorBehavior
{
    /// <summary>
    /// Regarding trailing directory separators, the path stays unmodified. A trailing directory separator will neither be added nor removed.
    /// </summary>
    None,
    /// <summary>
    /// Removes any trailing directory separators during normalization.
    /// </summary>
    Trim,
    /// <summary>
    /// Ensures the last character of a path is any directory separator. 
    /// </summary>
    /// <remarks>This normalization only specifies a valid system separator character is used but not which.</remarks>
    Ensure
}