namespace AnakinRaW.CommonUtilities.Collections;

public interface IFrugalValueListDictionary<TKey, TValue> : 
    IReadOnlyFrugalValueListDictionary<TKey, TValue>, 
    IValueListDictionary<TKey, TValue> 
    where TKey : notnull;