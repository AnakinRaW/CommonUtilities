using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a specialized dictionary that maps keys to lists of values, optimized for scenarios 
/// where the number of values per key is expected to be one.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the lists associated with the keys.</typeparam>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class FrugalValueListDictionary<TKey, TValue> 
    : ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>>, IFrugalValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <inheritdoc/>
    public new ImmutableFrugalList<TValue> this[TKey key] => GetValues(key);

    /// <summary>
    /// Initializes a new instance of the <see cref="FrugalValueListDictionary{TKey, TValue}"/> class
    /// that is empty and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    public FrugalValueListDictionary()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrugalValueListDictionary{TKey, TValue}"/> class
    /// that is empty and uses the specified <see cref="IEqualityComparer{T}"/>
    /// </summary>
    /// <param name="equalityComparer">
    /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys,
    /// or <see langword="null"/> to use the default <see cref="IEqualityComparer{T}"/> for the type of the key.
    /// </param>
    /// <remarks>
    /// This constructor allows customization of how keys are compared in the dictionary. 
    /// If no equality comparer is provided, the default comparer for the key type is used.
    /// </remarks>
    public FrugalValueListDictionary(IEqualityComparer<TKey>? equalityComparer) : base(equalityComparer)
    {
    }

    /// <summary>
    /// Creates a new instance of the value store specific to the implementation of the dictionary.
    /// </summary>
    /// <returns>
    /// A new <see cref="FrugalList{TValue}"/> instance to be used as the value store for the dictionary.
    /// </returns>
    protected override FrugalList<TValue> CreateValueStore()
    {
        return default;
    }

    /// <inheritdoc/>
    public new ImmutableFrugalList<TValue> GetValues(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
            return list.ToImmutableList();
        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    /// <inheritdoc/>
    public bool TryGetValues(TKey key, out ImmutableFrugalList<TValue> values)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
        {
            values = list.ToImmutableList();
            return true;
        }
        values = ImmutableFrugalList<TValue>.Empty;
        return false;
    }

    /// <summary>
    /// Creates a snapshot of the specified <see cref="FrugalList{TValue}"/>.
    /// </summary>
    /// <param name="list">The <see cref="FrugalList{TValue}"/> to create a snapshot from.</param>
    /// <returns>
    /// An immutable, read-only list containing the elements of the specified <see cref="FrugalList{TValue}"/>.
    /// </returns>
    protected override IReadOnlyList<TValue> CreateSnapshot(FrugalList<TValue> list)
    {
        return list.ToImmutableList();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The enumerator returns each key exactly once, paired with a <see cref="ImmutableFrugalList{T}"/> 
    /// containing all values associated with that key.
    /// </para>
    /// <para>
    /// Enumerators can be used to read the data in the collection, but they cannot be used to modify 
    /// the underlying collection.
    /// </para>
    /// </remarks>
    public new Enumerator GetEnumerator() => new(this, Enumerator.Frugal);

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>> IReadOnlyFrugalValueListDictionary<TKey, TValue>.GetEnumerator()
    {
        if (ValueCount == 0)
            return EmptyEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>>.Instance;
        return GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>.GetEnumerator()
    {
        if (ValueCount == 0)
            return EmptyEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>.Instance;
        return new Enumerator(this, Enumerator.AsReadOnly);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IReadOnlyValueListDictionary<TKey, TValue>)this).GetEnumerator();
    }
    
    /// <summary>
    /// Enumerates the elements of a <see cref="FrugalValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The enumerator provides a way to iterate through the key-value pairs in the dictionary, where each key is associated
    /// with an <see cref="ImmutableFrugalList{T}"/> containing all the values for that key.
    /// </para>
    /// <para>
    /// The enumerator is a value type and does not allocate additional memory during enumeration. It is designed to be
    /// efficient for scenarios where performance is critical.
    /// </para>
    /// <para>
    /// Modifying the dictionary while enumerating through it will invalidate the enumerator, and any subsequent operation
    /// on the enumerator will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public new struct Enumerator : 
        IEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>>,
        IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    {
        private readonly FrugalValueListDictionary<TKey, TValue> _dictionary;
        private readonly int _getEnumeratorRetType;

        internal const int Frugal = 1;
        internal const int AsReadOnly = 2;

        private readonly int _version;
        private readonly int _count;
        private int _index;

        private Entry _currentEntry;

        internal Enumerator(FrugalValueListDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
        {
            _dictionary = dictionary;
            _getEnumeratorRetType = getEnumeratorRetType;
            _count = dictionary.KeyOrderStore.Count;
            _index = 0;
            _currentEntry = default;
            _version = dictionary.Version;
        }

        /// <inheritdoc/>
        public KeyValuePair<TKey, ImmutableFrugalList<TValue>> Current => _currentEntry.AsFrugal();

        KeyValuePair<TKey, IReadOnlyList<TValue>> IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>.Current
            => _currentEntry.AsReadOnlyList();

        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index == _count + 1)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                if (_getEnumeratorRetType == AsReadOnly)
                    return _currentEntry.AsReadOnlyList();
                return _currentEntry.AsFrugal();
            }
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_version != _dictionary.Version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            
            if ((uint)_index < (uint)_count)
            {
                var key = _dictionary.KeyOrderStore[_index];

#if NET6_0_OR_GREATER
                ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary.ValueStore, key);
                _currentEntry = new Entry(key, list.ToImmutableList());
#else
                _currentEntry = new Entry(key, _dictionary.ValueStore[key].ToImmutableList());
#endif
                _index++;
                return true;
            }

            _index = _count + 1;
            _currentEntry = default;
            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            if (_version != _dictionary.Version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

            _index = 0;
            _currentEntry = default;
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }

    private readonly struct Entry(TKey key, ImmutableFrugalList<TValue> values)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, ImmutableFrugalList<TValue>> AsFrugal()
        {
            return new KeyValuePair<TKey, ImmutableFrugalList<TValue>>(key, values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, IReadOnlyList<TValue>> AsReadOnlyList()
        {
            return new KeyValuePair<TKey, IReadOnlyList<TValue>>(key, values);
        }
    }
}