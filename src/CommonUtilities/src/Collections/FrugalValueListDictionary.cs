using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace AnakinRaW.CommonUtilities.Collections;

[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("ValueCount = {ValueCount}")]
public class FrugalValueListDictionary<TKey, TValue> 
    : ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>>, IFrugalValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    public FrugalValueListDictionary(IEqualityComparer<TKey>? comparer = null) : base(comparer)
    {
    }

    protected override FrugalList<TValue> CreateValueStore()
    {
        return default;
    }

    public new ImmutableFrugalList<TValue> this[TKey key] => GetValues(key);

    public new ImmutableFrugalList<TValue> GetValues(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
            return list.ToImmutableList();
        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

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

    protected override IReadOnlyList<TValue> CreateSnapshot(FrugalList<TValue> list)
    {
        return list.ToImmutableList();
    }

    public new Enumerator GetEnumerator() => new(this, Enumerator.Frugal);

    IEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>> IReadOnlyFrugalValueListDictionary<TKey, TValue>.GetEnumerator()
    {
        if (ValueCount == 0)
            return EmptyEnumerator<KeyValuePair<TKey, ImmutableFrugalList<TValue>>>.Instance;
        return GetEnumerator();
    }

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

        public void Reset()
        {
            if (_version != _dictionary.Version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

            _index = 0;
            _currentEntry = default;
        }

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