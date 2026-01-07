using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a read-only generic collection that maps keys to lists of values,
/// using a memory-efficient representation optimized for one value per key.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IReadOnlyValueListDictionary{TKey, TValue}"/> with
/// methods that return <see cref="ImmutableFrugalList{T}"/>, a value type that provides
/// efficient storage for zero and one value.
/// </para>
/// <para>
/// All methods returning <see cref="ImmutableFrugalList{T}"/> return snapshots.
/// The returned lists are not affected by subsequent modifications to the dictionary.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IReadOnlyFrugalValueListDictionary<TKey, TValue> : 
    IReadOnlyValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the values associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose values to get.</param>
    /// <returns>
    /// An <see cref="ImmutableFrugalList{T}"/> containing all values for the specified key.
    /// </returns>
    /// <remarks>
    /// The returned list is a snapshot; subsequent modifications to the dictionary
    /// are not reflected in the returned list.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    new ImmutableFrugalList<TValue> this[TKey key] { get; }

    /// <summary>
    /// Gets the values associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose values to get.</param>
    /// <returns>
    /// An <see cref="ImmutableFrugalList{T}"/> containing all values for the specified key.
    /// </returns>
    /// <remarks>
    /// The returned list is a snapshot; subsequent modifications to the dictionary
    /// are not reflected in the returned list.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    new ImmutableFrugalList<TValue> GetValues(TKey key);

    /// <summary>
    /// Attempts to get the values associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose values to get.</param>
    /// <param name="values">
    /// When this method returns, contains an <see cref="ImmutableFrugalList{T}"/> of values
    /// associated with the specified key, if the key is found; otherwise, an empty list.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains at least one value with the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The returned list is a snapshot; subsequent modifications to the dictionary
    /// are not reflected in the returned list.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    bool TryGetValues(TKey key, out ImmutableFrugalList<TValue> values);

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>
    /// An enumerator that yields key-value pairs where each value is an 
    /// <see cref="ImmutableFrugalList{T}"/> of all values for that key.
    /// </returns>
    new IEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>> GetEnumerator();
}