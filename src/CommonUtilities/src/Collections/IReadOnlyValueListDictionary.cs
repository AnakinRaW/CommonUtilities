using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a read-only generic collection that maps keys to list of values.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a standard <see cref="IReadOnlyDictionary{TKey, TValue}"/>, this dictionary
/// allows multiple values to be associated with a single key.
/// </para>
/// <para>
/// When enumerating, each key appears exactly once with all its associated values
/// as a <see cref="ReadOnlyFrugalList{T}"/>.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface IReadOnlyValueListDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>> where TKey : notnull
{
    /// <summary>
    /// Gets the list of values associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the values to get.</param>
    /// <returns>A <see cref="ReadOnlyFrugalList{TValue}"/> containing all values for the specified key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    ReadOnlyFrugalList<TValue> this[TKey key] { get; }

    /// <summary>
    /// Gets a collection containing all values in the dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns a flattened collection of all values across all keys.
    /// If a key has multiple values, each value appears separately in the collection.
    /// </para>
    /// <para>
    /// Values appear in insertion order: first all values for the first key (in the order they were added),
    /// then all values for the second key, and so on.
    /// </para>
    /// <para>
    /// The collection count equals <see cref="Count"/>, not <see cref="KeyCount"/>.
    /// Modifications to the returned collection are not reflected in the dictionary.
    /// </para>
    /// <para>
    /// To get values for a specific key without flattening, use <see cref="GetValues(TKey)"/>.
    /// </para>
    /// </remarks>
    ICollection<TValue> Values { get; }

    /// <summary>
    /// Gets an <see cref="ICollection{T}"/> containing the keys in the dictionary.
    /// </summary>
    /// <remarks>
    /// Modifications to the collection are not reflected in the dictionary.
    /// <br/>
    /// The keys in the returned <see cref="ICollection{T}"/> are ordered by their first insertion into the dictionary.
    /// </remarks>
    ICollection<TKey> Keys { get; }

    /// <summary>
    /// Gets the total number of values across all keys in the dictionary.
    /// </summary>
    /// <remarks>
    /// This is the sum of all values for all keys, not the number of distinct keys.
    /// Use <see cref="KeyCount"/> to get the number of distinct keys.
    /// </remarks>
    int Count { get; }

    /// <summary>
    /// Gets the number of distinct keys in the dictionary.
    /// </summary>
    int KeyCount { get; }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns><see langword="true"/> if the dictionary contains the key; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Get a list of values stored with the specified key.
    /// </summary>
    /// <param name="key">The key to get the list of values for.</param>
    /// <returns>The list of values of the specified <paramref name="key"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    ReadOnlyFrugalList<TValue> GetValues(TKey key);

    /// <summary>
    /// Gets the last element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to get.</param>
    /// <returns>The last element with the specified key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    TValue GetLastValue(TKey key);

    /// <summary>
    /// Gets the first element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to get.</param>
    /// <returns>The first element with the specified key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    TValue GetFirstValue(TKey key);

    /// <summary>
    /// Gets the first value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">
    /// When this method returns, the first value associated with the specified key, if the key is found;
    /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the dictionary contains a value with the specified key; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool TryGetFirstValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Gets the last value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">
    /// When this method returns, the last value associated with the specified key, if the key is found;
    /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the dictionary contains a value with the specified key; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool TryGetLastValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Gets the list of values associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="values">
    /// When this method returns, a list of values associated with the specified key, if the key is found;
    /// otherwise, an empty list. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the dictionary contains at least one value with the specified key; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool TryGetValues(TKey key, out ReadOnlyFrugalList<TValue> values);
}