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
}