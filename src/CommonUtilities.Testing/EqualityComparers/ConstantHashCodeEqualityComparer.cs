using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Testing;

public sealed class ConstantHashCodeEqualityComparer<T>(IEqualityComparer<T> comparer) : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return comparer.Equals(x, y);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    public int GetHashCode(T obj)
    {
        return 42;
    }
}