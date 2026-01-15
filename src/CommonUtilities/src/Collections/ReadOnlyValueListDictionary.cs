using System;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a read-only, generic dictionary that maps keys to a list of values.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class ReadOnlyValueListDictionary<TKey, TValue> : ReadOnlyValueListDictionaryBase<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Gets an empty <see cref="ReadOnlyValueListDictionary{TKey,TValue}"/>.</summary>
    /// <value>An empty <see cref="ReadOnlyValueListDictionary{TKey,TValue}"/>.</value>
    /// <remarks>The returned instance is immutable and will always be empty.</remarks>
    public static ReadOnlyValueListDictionary<TKey, TValue> Empty { get; } = new(new ValueListDictionary<TKey, TValue>());

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyValueListDictionary{TKey, TValue}"/> class
    /// that is a wrapper around the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public ReadOnlyValueListDictionary(IReadOnlyValueListDictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }
}