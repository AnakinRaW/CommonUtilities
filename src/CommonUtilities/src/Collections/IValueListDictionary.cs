using System;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a generic collection that maps keys to list of values, while maintaining the order of key insertion.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value-list in the dictionary.</typeparam>
public interface IValueListDictionary<TKey, TValue> : IReadOnlyValueListDictionary<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Adds a value to the dictionary under the specified key.
    /// </summary>
    /// <remarks>
    /// Multiple values can be added under the same key. Values are stored in insertion order.
    /// </remarks>
    /// <param name="key">The key under which to add the value.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>
    /// <see langword="true"/> if the key already existed and the value was added 
    /// to an existing key; <see langword="false"/> if a new key was created.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool Add(TKey key, TValue value);

    /// <summary>
    /// Removes all values associated with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if the key was found and removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool Remove(TKey key);

    /// <summary>
    /// Removes a specific value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to remove.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the value was found and removed; 
    /// <see langword="false"/> if the key or value was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If this was the last value for the key, the key is also removed from the dictionary.
    /// If multiple identical values exist for the key, only the first occurrence is removed.
    /// </remarks>
    bool Remove(TKey key, TValue value);

    /// <summary>
    /// Removes all keys and values from the <see cref="IValueListDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IReadOnlyValueListDictionary{TKey, TValue}.Count"/> and 
    /// <see cref="IReadOnlyValueListDictionary{TKey, TValue}.KeyCount"/> are set to zero.
    /// </remarks>
    void Clear();

    ///// <summary>
    ///// Adds multiple values to the dictionary under the specified key.
    ///// </summary>
    ///// <param name="key">The key under which to add the values.</param>
    ///// <param name="values">The values to add.</param>
    ///// <remarks>
    ///// If <paramref name="values"/> is empty, the key is not created.
    ///// Values are appended in enumeration order.
    ///// </remarks>
    ///// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
    //void AddRange(TKey key, IEnumerable<TValue> values);

    ///// <summary>
    ///// Removes all values matching the predicate for the specified key.
    ///// </summary>
    ///// <param name="key">The key whose values to filter.</param>
    ///// <param name="match">The predicate that defines the conditions for removal.</param>
    ///// <returns>The number of values removed.</returns>
    ///// <remarks>
    ///// If all values for the key are removed, the key itself is also removed.
    ///// </remarks>
    ///// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="match"/> is <see langword="null"/>.</exception>
    //int RemoveAll(TKey key, Predicate<TValue> match);
}