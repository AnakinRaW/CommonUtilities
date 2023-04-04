using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public struct ProgressType : IEquatable<ProgressType>
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public bool Equals(ProgressType other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProgressType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}