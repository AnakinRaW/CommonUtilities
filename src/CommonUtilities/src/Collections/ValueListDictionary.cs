using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a generic dictionary that maps keys to one or more values, while maintaining the order of key insertion.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// Unlike a standard <see cref="Dictionary{TKey, TValue}"/>, this dictionary allows multiple values 
/// to be associated with a single key. Values are stored in the order they were added.
/// </para>
/// <para>
/// A <see cref="ValueListDictionary{TKey,TValue}"/> can support multiple readers concurrently, 
/// as long as the collection is not modified. Even so, enumerating through a collection is 
/// intrinsically not a thread-safe procedure. In the rare case where an enumeration contends 
/// with write accesses, the collection must be locked during the entire enumeration.
/// </para>
/// </remarks>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class ValueListDictionary<TKey, TValue> : IValueListDictionary<TKey, TValue> where TKey : notnull
{
    private readonly List<TKey> _keyOrder = [];
    private readonly Dictionary<TKey, FrugalList<TValue>> _values;

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public int KeyCount => _keyOrder.Count;

    /// <summary>
    /// Gets a collection containing the keys in the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <value>
    /// A <see cref="KeyCollection"/> containing the keys in the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// The keys in the <see cref="KeyCollection"/> are returned in the order they were first inserted.
    /// </para>
    /// <para>
    /// The returned <see cref="KeyCollection"/> is not a static copy; instead, the <see cref="KeyCollection"/> 
    /// refers back to the keys in the original <see cref="ValueListDictionary{TKey, TValue}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionary{TKey, TValue}"/> continue to be 
    /// reflected in the <see cref="KeyCollection"/>.
    /// </para>
    /// </remarks>
    public KeyCollection Keys => field ??= new KeyCollection(this);

    /// <inheritdoc />
    ICollection<TKey> IReadOnlyValueListDictionary<TKey, TValue>.Keys => field ??= new KeyCollection(this);

    /// <inheritdoc />
    public ReadOnlyFrugalList<TValue> this[TKey key] => GetValues(key);

    /// <summary>
    /// Gets a collection containing all values in the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <value>
    /// A <see cref="ValueCollection"/> containing all values in the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </value>
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
    /// </para>
    /// <para>
    /// The returned <see cref="ValueCollection"/> is not a static copy; instead, it 
    /// refers back to the values in the original <see cref="ValueListDictionary{TKey, TValue}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionary{TKey, TValue}"/> continue to be 
    /// reflected in the <see cref="ValueCollection"/>.
    /// </para>
    /// <para>
    /// To get values for a specific key without flattening, use <see cref="GetValues(TKey)"/>.
    /// </para>
    /// </remarks>
    public ValueCollection Values => field ??= new ValueCollection(this);

    /// <inheritdoc />
    ICollection<TValue> IReadOnlyValueListDictionary<TKey, TValue>.Values => Values;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionary{TKey, TValue}"/> class 
    /// that is empty and uses the default equality comparer for the key type.
    /// </summary>
    public ValueListDictionary() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionary{TKey, TValue}"/> class 
    /// that is empty and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="comparer">
    /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, 
    /// or <see langword="null"/> to use the default <see cref="EqualityComparer{T}"/> for the type of the key.
    /// </param>
    public ValueListDictionary(IEqualityComparer<TKey>? comparer)
    {
        _values = new Dictionary<TKey, FrugalList<TValue>>(comparer ?? EqualityComparer<TKey>.Default);
    }

    /// <inheritdoc />
    public bool Add(TKey key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Count++;

#if NET
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_values, key, out var exists);
        list.Add(value);

        if (!exists)
            _keyOrder.Add(key);

        return exists;
#else
        if (_values.TryGetValue(key, out var list))
        {
            list.Add(value);
            _values[key] = list;
            return true;
        }

        _keyOrder.Add(key);
        _values[key] = new FrugalList<TValue>(value);
        return false;
#endif
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (_values.TryGetValue(key, out var list))
        {
            Count -= list.Count;
            _values.Remove(key);
            _keyOrder.Remove(key);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Remove(TKey key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

# if NET6_0_OR_GREATER
        ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(_values, key);
        if (Unsafe.IsNullRef(ref list))
            return false;

        if (!list.Remove(value))
            return false;

        Count--;

        // If this was the last value, remove the key entirely
        if (list.Count == 0)
        {
            _values.Remove(key);
            _keyOrder.Remove(key);
        }

        return true;
# else
        if (!_values.TryGetValue(key, out var list))
            return false;

        if (!list.Remove(value))
            return false;

        Count--;

        // If this was the last value, remove the key entirely
        if (list.Count == 0)
        {
            _values.Remove(key);
            _keyOrder.Remove(key);
        }
        else
        {
            _values[key] = list;
        }

        return true;
# endif
    }

    /// <inheritdoc />
    public void Clear()
    {
        _keyOrder.Clear();
        _values.Clear();
        Count = 0;
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _values.ContainsKey(key);

    /// <inheritdoc />
    public ReadOnlyFrugalList<TValue> GetValues(TKey key)
    {
        if (_values.TryGetValue(key, out var list))
            return list.AsReadOnly();

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    /// <inheritdoc />
    public TValue GetFirstValue(TKey key)
    {
        if (_values.TryGetValue(key, out var list))
            return list[0];

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    /// <inheritdoc />
    public TValue GetLastValue(TKey key)
    {
        if (_values.TryGetValue(key, out var list))
#if NETSTANDARD2_1_OR_GREATER || NET
            return list[^1];
#else
            return list[list.Count - 1];
#endif

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    /// <inheritdoc />
    public bool TryGetFirstValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_values.TryGetValue(key, out var list))
        {
            value = list[0];
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetLastValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_values.TryGetValue(key, out var list))
        {
#if NETSTANDARD2_1_OR_GREATER || NET
            value = list[^1];
#else
            value = list[list.Count - 1];
#endif
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetValues(TKey key, out ReadOnlyFrugalList<TValue> values)
    {
        if (_values.TryGetValue(key, out var list))
        {
            values = list.AsReadOnly();
            return true;
        }

        values = ReadOnlyFrugalList<TValue>.Empty;
        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the <see cref="ValueListDictionary{TKey, TValue}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The enumerator returns each key exactly once, paired with a <see cref="ReadOnlyFrugalList{T}"/> 
    /// containing all values associated with that key.
    /// </para>
    /// <para>
    /// Enumerators can be used to read the data in the collection, but they cannot be used to modify 
    /// the underlying collection.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>> IEnumerable<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerates the elements of a <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Each element is a <see cref="KeyValuePair{TKey, TValue}"/> where the key is unique 
    /// and the value is a <see cref="ReadOnlyFrugalList{T}"/> containing all values for that key.
    /// </remarks>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>>
    {
        private readonly ValueListDictionary<TKey, TValue> _dictionary;
        private int _index;
        private KeyValuePair<TKey, ReadOnlyFrugalList<TValue>> _current;

        internal Enumerator(ValueListDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, ReadOnlyFrugalList<TValue>> Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index == _dictionary._keyOrder.Count + 1)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                return Current;
            }
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_index < _dictionary._keyOrder.Count)
            {
                var key = _dictionary._keyOrder[_index];
                _current = new KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>(
                    key,
                    _dictionary._values[key].AsReadOnly());
                _index++;
                return true;
            }

            _index = _dictionary._keyOrder.Count + 1;
            _current = default;
            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public void Dispose() { }
    }

    /// <summary>
    /// Represents the collection of keys in a <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The keys in the <see cref="KeyCollection"/> are returned in the order they were first inserted 
    /// into the <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </para>
    /// <para>
    /// The <see cref="KeyCollection"/> is not a static copy; instead, the <see cref="KeyCollection"/> 
    /// refers back to the keys in the original <see cref="ValueListDictionary{TKey, TValue}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionary{TKey, TValue}"/> continue to be 
    /// reflected in the <see cref="KeyCollection"/>.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        private readonly ValueListDictionary<TKey, TValue> _dictionary;

        internal KeyCollection(ValueListDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="KeyCollection"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="KeyCollection"/>.</value>
        public int Count => _dictionary._keyOrder.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="KeyCollection"/> is read-only.
        /// </summary>
        /// <value>Always returns <see langword="true"/>.</value>
        public bool IsReadOnly => true;

        /// <summary>
        /// Determines whether the <see cref="KeyCollection"/> contains a specific key.
        /// </summary>
        /// <param name="item">The key to locate in the <see cref="KeyCollection"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in the <see cref="KeyCollection"/>; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(TKey item) => _dictionary.ContainsKey(item);

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            _dictionary._keyOrder.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="KeyCollection"/>.
        /// </summary>
        /// <returns>A <see cref="List{T}.Enumerator"/> for the <see cref="KeyCollection"/>.</returns>
        public List<TKey>.Enumerator GetEnumerator() => _dictionary._keyOrder.GetEnumerator();

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();
    }

    /// <summary>
    /// Represents the collection of values in a <see cref="ValueListDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values in the <see cref="ValueCollection"/> are returned grouped by key, in the order 
    /// the keys were first inserted into the <see cref="ValueListDictionary{TKey, TValue}"/>. 
    /// Within each key group, values appear in the order they were added.
    /// </para>
    /// <para>
    /// The <see cref="ValueCollection"/> is not a static copy; instead, the <see cref="ValueCollection"/> 
    /// refers back to the values in the original <see cref="ValueListDictionary{TKey, TValue}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionary{TKey, TValue}"/> continue to be 
    /// reflected in the <see cref="ValueCollection"/>.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly ValueListDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(ValueListDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <inheritdoc cref="ICollection{T}.Count" />
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="ValueCollection"/> is read-only.
        /// </summary>
        /// <value>Always returns <see langword="true"/>.</value>
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public bool Contains(TValue item)
        {
            var comparer = EqualityComparer<TValue>.Default;
            foreach (var value in this)
            {
                if (comparer.Equals(value, item))
                    return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var value in this)
                array[arrayIndex++] = value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ValueCollection"/>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> for the <see cref="ValueCollection"/>.</returns>
        public Enumerator GetEnumerator() => new(_dictionary);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// This operation is not supported on a read-only collection.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Enumerates the elements of a <see cref="ValueCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private ValueListDictionary<TKey, TValue>.Enumerator _dictEnumerator;
            private FrugalList<TValue>.FrugalEnumerator _valueEnumerator;
            private bool _hasCurrentList;

            internal Enumerator(ValueListDictionary<TKey, TValue> dictionary)
            {
                _dictEnumerator = dictionary.GetEnumerator();
                _valueEnumerator = default;
                _hasCurrentList = false;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            /// <value>The element in the <see cref="ValueCollection"/> at the current position of the enumerator.</value>
            public TValue Current => _valueEnumerator.Current;

            /// <inheritdoc />
            object IEnumerator.Current => Current!;

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="ValueCollection"/>.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; 
            /// <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                // Try next value in current list
                if (_hasCurrentList && _valueEnumerator.MoveNext())
                    return true;

                // Move to next key-value group
                while (_dictEnumerator.MoveNext())
                {
                    _valueEnumerator = _dictEnumerator.Current.Value.GetEnumerator();
                    _hasCurrentList = true;

                    if (_valueEnumerator.MoveNext())
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _dictEnumerator.Reset();
                _valueEnumerator = default;
                _hasCurrentList = false;
            }

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose() => _dictEnumerator.Dispose();
        }
    }
}