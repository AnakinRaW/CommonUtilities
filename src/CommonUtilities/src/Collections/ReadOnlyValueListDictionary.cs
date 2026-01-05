using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AnakinRaW.CommonUtilities.Collections;

public class ReadOnlyValueListDictionary<TKey, TValue>(IValueListDictionary<TKey, TValue> dictionary)
    : ReadOnlyValueListDictionaryBase<TKey, TValue>(dictionary)
    where TKey : notnull
{
    /// <summary>Gets an empty <see cref="IReadOnlyValueListDictionary{TKey,TValue}"/>.</summary>
    /// <value>An empty <see cref="IReadOnlyValueListDictionary{TKey,TValue}"/>.</value>
    /// <remarks>The returned instance is immutable and will always be empty.</remarks>
    public static ReadOnlyValueListDictionary<TKey, TValue> Empty { get; } = new(new ValueListDictionary<TKey, TValue>());
}

public class ReadOnlyFrugalValueListDictionary<TKey, TValue>(IValueListDictionary<TKey, TValue> dictionary)
    : ReadOnlyValueListDictionaryBase<TKey, TValue>(dictionary), IReadOnlyFrugalValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Gets an empty <see cref="IReadOnlyValueListDictionary{TKey,TValue}"/>.</summary>
    /// <value>An empty <see cref="IReadOnlyValueListDictionary{TKey,TValue}"/>.</value>
    /// <remarks>The returned instance is immutable and will always be empty.</remarks>
    public static ReadOnlyFrugalValueListDictionary<TKey, TValue> Empty { get; } = new(new FrugalValueListDictionary<TKey, TValue>());

    public ReadOnlyFrugalList<TValue> this[TKey key] => throw new NotImplementedException();

    public ReadOnlyFrugalList<TValue> GetValues(TKey key)
    {
        if (Dictionary is FrugalValueListDictionary<TKey, TValue> frugalDict)
            return frugalDict.GetValues(key);
        return new ReadOnlyFrugalList<TValue>(Dictionary.GetValues(key));
    }

    public bool TryGetValues(TKey key, out ReadOnlyFrugalList<TValue> values)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>> GetEnumerator()
    {
        throw new NotImplementedException();
    }
}



/// <summary>
/// Represents a read-only, generic dictionary that maps keys to one or more values, while maintaining the order of key insertion.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public abstract class ReadOnlyValueListDictionaryBase<TKey, TValue> : IReadOnlyValueListDictionary<TKey, TValue> where TKey : notnull
{
    protected readonly IValueListDictionary<TKey, TValue> Dictionary;
    
    /// <inheritdoc />
    public IReadOnlyList<TValue> this[TKey key] => Dictionary[key];

    /// <summary>
    /// Gets a key collection that contains the keys of the dictionary.
    /// </summary>
    public KeyCollection Keys => field ??= new KeyCollection(Dictionary.Keys);

    /// <summary>
    /// Gets a collection that contains the values in the dictionary.
    /// </summary>
    public ValueCollection Values => field ??= new ValueCollection(Dictionary.Values);

    ICollection<TValue> IReadOnlyValueListDictionary<TKey, TValue>.Values => Values;

    ICollection<TKey> IReadOnlyValueListDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    public int Count => Dictionary.Count;

    /// <inheritdoc />
    public int KeyCount => Dictionary.KeyCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyValueListDictionaryBase{TKey,TValue}"/> class that is a wrapper around the specified value list dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    protected ReadOnlyValueListDictionaryBase(IValueListDictionary<TKey, TValue> dictionary)
    {
        Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);

    /// <inheritdoc />
    public IReadOnlyList<TValue> GetValues(TKey key) => Dictionary.GetValues(key);

    /// <inheritdoc />
    public TValue GetLastValue(TKey key) => Dictionary.GetLastValue(key);

    /// <inheritdoc />
    public TValue GetFirstValue(TKey key) => Dictionary.GetFirstValue(key);

    /// <inheritdoc />
    public bool TryGetFirstValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Dictionary.TryGetFirstValue(key, out value);

    /// <inheritdoc />
    public bool TryGetLastValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Dictionary.TryGetLastValue(key, out value);

    /// <inheritdoc />
    public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values) => Dictionary.TryGetValues(key, out values);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> GetEnumerator() => Dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Represents a read-only collection of the keys of a <see cref="ReadOnlyValueListDictionaryBase{TKey,TValue}"/> object.
    /// </summary>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        private readonly ICollection<TKey> _collection;

        /// <inheritdoc cref="ICollection{TKey}.Count" />
        public int Count => _collection.Count;

        bool ICollection<TKey>.IsReadOnly => true;

        internal KeyCollection(ICollection<TKey> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        /// <inheritdoc />
        public bool Contains(TKey item) => _collection.Contains(item);

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<TKey> GetEnumerator() => _collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();
    }

    /// <summary>
    /// Represents a read-only collection of the values of a <see cref="ReadOnlyValueListDictionaryBase{TKey,TValue}"/> object.
    /// </summary>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly ICollection<TValue> _collection;

        /// <inheritdoc cref="ICollection{TKey}.Count" />
        public int Count => _collection.Count;

        bool ICollection<TValue>.IsReadOnly => true;

        internal ValueCollection(ICollection<TValue> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        bool ICollection<TValue>.Contains(TValue item) => _collection.Contains(item);

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator() => _collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();

        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();
    }
}