using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;
#endif


namespace AnakinRaW.CommonUtilities.Collections;

[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class FrugalValueListDictionary<TKey, TValue> 
    : ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>>, IFrugalValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    protected override FrugalList<TValue> CreateValueStore()
    {
        return default;
    }

    public new ReadOnlyFrugalList<TValue> this[TKey key] => GetValues(key);

    public new ReadOnlyFrugalList<TValue> GetValues(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
            return list.AsReadOnly();
        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public bool TryGetValues(TKey key, out ReadOnlyFrugalList<TValue> values)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (ValueStore.TryGetValue(key, out var list))
        {
            values = list.AsReadOnly();
            return true;
        }
        values = ReadOnlyFrugalList<TValue>.Empty;
        return false;
    }

    IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>> IReadOnlyFrugalValueListDictionary<TKey, TValue>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public new FrugalEnumerator GetEnumerator() => new(this);
    
    protected override IReadOnlyList<TValue> CreateReadOnlyWrapper(FrugalList<TValue> list)
    {
        return new ReadOnlyFrugalList<TValue>(list);
    }

    public struct FrugalEnumerator : IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>>
    {
        private readonly List<TKey> _keyOrder;
        private readonly Dictionary<TKey, FrugalList<TValue>> _values;
        private readonly int _count;
        private int _index;
        private KeyValuePair<TKey, ReadOnlyFrugalList<TValue>> _current;

        internal FrugalEnumerator(FrugalValueListDictionary<TKey, TValue> dictionary)
        {
            _keyOrder = dictionary.KeyOrderStore;
            _values = dictionary.ValueStore;
            _count = _keyOrder.Count;
            _index = 0;
            _current = default;
        }

        public KeyValuePair<TKey, ReadOnlyFrugalList<TValue>> Current => _current;

        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index == _count + 1)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                return _current;
            }
        }

        public bool MoveNext()
        {
            if (_index < _count)
            {
                var key = _keyOrder[_index];

#if NET6_0_OR_GREATER
                ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(_values, key);
                _current = new KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>(
                    key,
                    list.AsReadOnly());
#else
                _current = new KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>(
                    key,
                    _values[key].AsReadOnly());
#endif
                _index++;
                return true;
            }

            _index = _count + 1;
            _current = default;
            return false;
        }

        public void Reset()
        {
            _index = 0;
            _current = default;
        }

        public void Dispose()
        {
        }
    }
}