using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Testing;

/// <summary>
/// Provides an equality comparer for objects of type <typeparamref name="T"/> that always returns a constant hash code.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
/// <remarks>
/// This comparer uses a constant hash code for all objects, which can be useful for testing scenarios where hash code collisions need to be simulated.
/// </remarks>
public sealed class ConstantHashCodeEqualityComparer<T>(IEqualityComparer<T> comparer) : IEqualityComparer<T>
{
    /// <summary>
    /// Determines whether the specified objects are equal.
    /// </summary>
    /// <remarks>
    /// This method delegates the equality comparison to the underlying comparer provided during the construction of the <see cref="ConstantHashCodeEqualityComparer{T}"/>.
    /// </remarks>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns><see langword="true"/> if the specified objects are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(T? x, T? y)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return comparer.Equals(x, y);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    /// <summary>
    /// Returns a constant hash code for the specified object.
    /// </summary>
    /// <remarks>
    /// This method always returns the same hash code value (42) regardless of the input object.
    /// </remarks>
    /// <param name="obj">The object for which the hash code is to be generated.</param>
    /// <returns>A constant hash code value.</returns>
    public int GetHashCode(T obj)
    {
        return 42;
    }
}