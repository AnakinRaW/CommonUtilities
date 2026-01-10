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

internal sealed class EmptyEnumerator<T> : IEnumerator<T>
{
    public static readonly EmptyEnumerator<T> Instance = new();

    public T Current => throw new InvalidOperationException();

    object? IEnumerator.Current => Current;

    private EmptyEnumerator() { }

    public bool MoveNext()
    {
        return false;
    }

    public void Reset() { }

    public void Dispose() { }
}

/// <summary>
/// Provides a base class for a generic collection that maps keys to lists of values.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must not be <see langword="null"/>.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the lists associated with each key.</typeparam>
/// <typeparam name="TList">The type of the list used to store values for each key.</typeparam>
/// <remarks>
/// This class serves as an abstract base for collections that associate keys with multiple values stored in lists.
/// It ensures that keys are unique and maintains the order of value insertion within each list.
/// </remarks>
[DebuggerDisplay("Count = {Count}, KeyCount = {KeyCount}")]
public abstract class ValueListDictionaryBase<TKey, TValue, TList> : IValueListDictionary<TKey, TValue>
    where TKey : notnull
    where TList : IList<TValue>
{
    private int _version;

    protected readonly List<TKey> KeyOrderStore = [];
    protected readonly Dictionary<TKey, TList> ValueStore;

    protected int Version => _version;

    /// <inheritdoc />
    public IReadOnlyList<TValue> this[TKey key] => GetValues(key);

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public int KeyCount => KeyOrderStore.Count;

    /// <summary>
    /// Gets a collection containing all values in the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <value>
    /// A <see cref="ValueCollection"/> containing all values in the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
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
    /// refers back to the values in the original <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> continue to be 
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
    /// Gets a collection containing the keys in the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <value>
    /// A <see cref="KeyCollection"/> containing the keys in the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// The keys in the <see cref="KeyCollection"/> are returned in the order they were first inserted.
    /// </para>
    /// <para>
    /// The returned <see cref="KeyCollection"/> is not a static copy; instead, the <see cref="KeyCollection"/> 
    /// refers back to the keys in the original <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> continue to be 
    /// reflected in the <see cref="KeyCollection"/>.
    /// </para>
    /// </remarks>
    public KeyCollection Keys => field ??= new KeyCollection(this);

    ICollection<TKey> IReadOnlyValueListDictionary<TKey, TValue>.Keys => Keys;


    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> class 
    /// that is empty and uses the default equality comparer for the key type.
    /// </summary>
    protected ValueListDictionaryBase() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> class 
    /// that is empty and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="comparer">
    /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, 
    /// or <see langword="null"/> to use the default <see cref="EqualityComparer{T}"/> for the type of the key.
    /// </param>
    protected ValueListDictionaryBase(IEqualityComparer<TKey>? comparer)
    {
        ValueStore = new Dictionary<TKey, TList>(comparer ?? EqualityComparer<TKey>.Default);
    }

    protected abstract TList CreateValueStore();

    protected abstract IReadOnlyList<TValue> CreateSnapshot(TList list);

    protected virtual void OnAfterValueListModified(TKey key, TList list)
    {
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        return ValueStore.ContainsKey(key);
    }

    public IReadOnlyList<TValue> GetValues(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
            return CreateSnapshot(list);
        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public TValue GetLastValue(TKey key)
    {
        if (key == null) 
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
#if NETSTANDARD2_1_OR_GREATER || NET
            return list[^1];
#else
            return list[list.Count - 1];
#endif

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public TValue GetFirstValue(TKey key)
    {
        if (key == null) 
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
            return list[0];
        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public bool TryGetFirstValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (key == null) 
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
        {
            value = list[0];
            return true;
        }

        value = default!;
        return false;
    }

    public bool TryGetLastValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
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

    public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
        {
            values = CreateSnapshot(list);
            return true;
        }

        values = [];
        return false;
    }

    public bool Add(TKey key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Count++;

#if NET6_0_OR_GREATER
        ref var valueList = ref CollectionsMarshal.GetValueRefOrAddDefault(ValueStore, key, out var exists);
        if (!exists)
        {
            valueList = CreateValueStoreInternal();
            KeyOrderStore.Add(key);
            _version++;
        }
        Debug.Assert(valueList is not null);
        
        valueList.Add(value);
        OnAfterValueListModified(key, valueList);
        
        return exists;
#else
        var exists = ValueStore.TryGetValue(key, out var valueList);
        if (!exists)
        {
            valueList = CreateValueStoreInternal();
            KeyOrderStore.Add(key);
            _version++;
        }

        valueList!.Add(value);
        OnAfterValueListModified(key, valueList);

        if (typeof(TList).IsValueType || !exists)
            ValueStore[key] = valueList;
        return exists;
#endif
    }


    private TList CreateValueStoreInternal()
    {
        return CreateValueStore() ?? throw new InvalidOperationException("value store cannot be null");
    }

    public bool Remove(TKey key)
    {
        if (key == null) 
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
        {
            Count -= list.Count;
            ValueStore.Remove(key);
            KeyOrderStore.Remove(key);
            _version++;
            return true;
        }

        return false;
    }

    public bool Remove(TKey key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

#if NET6_0_OR_GREATER
        ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(ValueStore, key);
        if (Unsafe.IsNullRef(ref list))
            return false;

        if (!list.Remove(value))
            return false;

        Count--;
        OnAfterValueListModified(key, list);

        if (list.Count == 0)
        {
            ValueStore.Remove(key);
            KeyOrderStore.Remove(key);
            _version++;
        }

        return true;
#else
        if (!ValueStore.TryGetValue(key, out var list))
            return false;

        if (!list.Remove(value))
            return false;

        Count--;
        OnAfterValueListModified(key, list);

        if (list.Count == 0)
        {
            ValueStore.Remove(key);
            KeyOrderStore.Remove(key);
            _version++;
        }
        else if (typeof(TList).IsValueType)
        {
            ValueStore[key] = list;
        }

        return true;
#endif
    }

    public void Clear()
    {
        if (KeyOrderStore.Count > 0)
        {
            KeyOrderStore.Clear();
            ValueStore.Clear();
            Count = 0;
            _version++;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The enumerator returns each key exactly once, paired with a <see cref="IReadOnlyList{T}"/> 
    /// containing all values associated with that key.
    /// </para>
    /// <para>
    /// Enumerators can be used to read the data in the collection, but they cannot be used to modify 
    /// the underlying collection.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>.
        GetEnumerator() => Count == 0
            ? EmptyEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>.Instance
            : GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>)this).GetEnumerator();

    /// <summary>
    /// Enumerates the elements of a <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <remarks>
    /// Each element is a <see cref="KeyValuePair{TKey, TValue}"/> where the key is unique 
    /// and the value is a <see cref="IReadOnlyList{T}"/> containing all values for that key.
    /// </remarks>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    {
        private readonly ValueListDictionaryBase<TKey, TValue, TList> _dictionary;
        private readonly int _version;
        private int _index;
        private KeyValuePair<TKey, IReadOnlyList<TValue>> _current;

        internal Enumerator(ValueListDictionaryBase<TKey, TValue, TList> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary.Version;
            _index = 0;
            _current = default;
        }

        public KeyValuePair<TKey, IReadOnlyList<TValue>> Current => _current;

        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index == _dictionary.KeyCount + 1)
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                return Current;
            }
        }

        public bool MoveNext()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

            var keyOrder = _dictionary.KeyOrderStore;

            while ((uint)_index < (uint)keyOrder.Count)
            {
                var key = keyOrder[_index++];
                var snapshot = _dictionary.CreateSnapshot(_dictionary.ValueStore[key]);
                _current = new KeyValuePair<TKey, IReadOnlyList<TValue>>(key, snapshot);
                return true;
            }
            
            _index = keyOrder.Count + 1;
            _current = default;
            return false;
        }

        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

            _index = 0;
            _current = default;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Represents the collection of keys in a <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The keys in the <see cref="KeyCollection"/> are returned in the order they were first inserted 
    /// into the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </para>
    /// <para>
    /// The <see cref="KeyCollection"/> is not a static copy; instead, the <see cref="KeyCollection"/> 
    /// refers back to the keys in the original <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> continue to be 
    /// reflected in the <see cref="KeyCollection"/>.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        private readonly ValueListDictionaryBase<TKey, TValue, TList> _dictionary;

        internal KeyCollection(ValueListDictionaryBase<TKey, TValue, TList> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="KeyCollection"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="KeyCollection"/>.</value>
        public int Count => _dictionary.KeyOrderStore.Count;

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

            _dictionary.KeyOrderStore.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="KeyCollection"/>.
        /// </summary>
        /// <returns>A <see cref="List{T}.Enumerator"/> for the <see cref="KeyCollection"/>.</returns>
        public List<TKey>.Enumerator GetEnumerator() => _dictionary.KeyOrderStore.GetEnumerator();

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => Count == 0 
                ? EmptyEnumerator<TKey>.Instance 
                : GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TKey>)this).GetEnumerator();

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
    /// Represents the collection of values in a <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values in the <see cref="ValueCollection"/> are returned grouped by key, in the order 
    /// the keys were first inserted into the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>. 
    /// Within each key group, values appear in the order they were added.
    /// </para>
    /// <para>
    /// The <see cref="ValueCollection"/> is not a static copy; instead, the <see cref="ValueCollection"/> 
    /// refers back to the values in the original <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/>. 
    /// Therefore, changes to the <see cref="ValueListDictionaryBase{TKey, TValue, TList}"/> continue to be 
    /// reflected in the <see cref="ValueCollection"/>.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly ValueListDictionaryBase<TKey, TValue, TList> _dictionary;

        internal ValueCollection(ValueListDictionaryBase<TKey, TValue, TList> dictionary)
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
            foreach (var list in _dictionary.ValueStore.Values)
            {
                if (list.Contains(item))
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
        /// <returns>
        /// An enumerator for the <see cref="ValueCollection"/>.
        /// </returns>
        public Enumerator GetEnumerator() => new(_dictionary);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            if (Count == 0)
                return EmptyEnumerator<TValue>.Instance;
            return new Enumerator(_dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TValue>)this).GetEnumerator();

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
        /// <summary>
        /// Enumerates the elements of a <see cref="ValueCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly ValueListDictionaryBase<TKey, TValue, TList> _dictionary;
            private readonly int _version;
            private TList? _currentList;
            private int _keyIndex;
            private int _valueIndex;
            private TValue _current;

            internal Enumerator(ValueListDictionaryBase<TKey, TValue, TList> dictionary)
            {
                _dictionary = dictionary;
                _currentList = default;
                _keyIndex = 0;
                _valueIndex = -1;
                _current = default!;
                _version = dictionary._version;
            }

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public TValue Current => _current;

            /// <inheritdoc />
            object? IEnumerator.Current
            {
                get
                {
                    if (_valueIndex < 0)
                        throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                    return _current;
                }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                // Try to advance within current cached list
                if (_valueIndex >= 0)
                {
                    _valueIndex++;
                    if (_valueIndex < _currentList!.Count)
                    {
                        _current = _currentList[_valueIndex];
                        return true;
                    }
                    // Current list exhausted, move to next key
                    _keyIndex++;
                }

                // Find next non-empty list
                return MoveToNextKey();
            }

            private bool MoveToNextKey()
            {
                var keyOrder = _dictionary.KeyOrderStore;
                var values = _dictionary.ValueStore;

                if (_keyIndex < keyOrder.Count)
                {
                    var key = keyOrder[_keyIndex];
                    _currentList = values[key];

                    Debug.Assert(_currentList.Count > 0);

                    _valueIndex = 0;
                    _current = _currentList[0];
                    return true;
                }

                _valueIndex = -1;
                _current = default!;
                return false;
            }

            /// <inheritdoc />
            public void Reset()
            {
                if (_version != _dictionary._version)
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                _currentList = default;
                _keyIndex = 0;
                _valueIndex = -1;
                _current = default!;
            }

            /// <inheritdoc />
            public void Dispose() { }
        }
    }
}