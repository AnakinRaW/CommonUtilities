using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Collections;

public interface IReadOnlyFrugalValueListDictionary<TKey, TValue> : 
    IReadOnlyValueListDictionary<TKey, TValue>
    where TKey : notnull
{
    new ReadOnlyFrugalList<TValue> this[TKey key] { get; }

    new ReadOnlyFrugalList<TValue> GetValues(TKey key);

    bool TryGetValues(TKey key, out ReadOnlyFrugalList<TValue> values);
    
    new IEnumerator<KeyValuePair<TKey, ReadOnlyFrugalList<TValue>>> GetEnumerator();
}