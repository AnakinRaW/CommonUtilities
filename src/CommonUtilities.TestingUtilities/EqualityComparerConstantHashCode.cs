using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Testing;

public sealed class EqualityComparerConstantHashCode<T>(IEqualityComparer<T> comparer) : IEqualityComparer<T>
{
    public bool Equals(T x, T y) => comparer.Equals(x, y);

    public int GetHashCode(T obj) => 42;
}