using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Represents a named progress type which can be used as a filter channel.
/// </summary>
public readonly struct ProgressType : IEquatable<ProgressType>
{
    /// <summary>
    /// Gets the unique identifier of the progress type.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the progress type.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <inheritdoc/>
    public bool Equals(ProgressType other)
    {
        return Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ProgressType other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Compares two values to determine equality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ProgressType left, ProgressType right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is not equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ProgressType left, ProgressType right)
    {
        return !(left == right);
    }
}