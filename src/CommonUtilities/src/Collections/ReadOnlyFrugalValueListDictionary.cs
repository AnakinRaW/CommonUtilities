using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a read-only dictionary that maps keys to immutable frugal lists of values.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class ReadOnlyFrugalValueListDictionary<TKey, TValue>
    : ReadOnlyValueListDictionaryBase<TKey, TValue>, IReadOnlyFrugalValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly IReadOnlyFrugalValueListDictionary<TKey, TValue> _frugalValueList;

    /// <summary>Gets an empty <see cref="ReadOnlyFrugalValueListDictionary{TKey,TValue}"/>.</summary>
    /// <value>An empty <see cref="ReadOnlyFrugalValueListDictionary{TKey,TValue}"/>.</value>
    /// <remarks>The returned instance is immutable and will always be empty.</remarks>
    public static ReadOnlyFrugalValueListDictionary<TKey, TValue> Empty { get; } =
        new(new FrugalValueListDictionary<TKey, TValue>());

    /// <inheritdoc/>
    public new ImmutableFrugalList<TValue> this[TKey key] => GetValues(key);

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyFrugalValueListDictionary{TKey, TValue}"/> class
    /// that is a wrapper around the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public ReadOnlyFrugalValueListDictionary(IReadOnlyFrugalValueListDictionary<TKey, TValue> dictionary)
        : base(dictionary)
    {
        _frugalValueList = dictionary;
    }

    /// <inheritdoc/>
    public new ImmutableFrugalList<TValue> GetValues(TKey key)
    {
        return _frugalValueList.GetValues(key);
    }

    /// <inheritdoc/>
    public bool TryGetValues(TKey key, out ImmutableFrugalList<TValue> values)
    {
        return _frugalValueList.TryGetValues(key, out values);
    }

    /// <inheritdoc/>
    public new IEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>> GetEnumerator()
    {
        return _frugalValueList.GetEnumerator();
    }
}