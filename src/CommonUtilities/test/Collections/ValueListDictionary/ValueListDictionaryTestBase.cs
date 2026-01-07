using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ValueListDictionaryTestBase<TKey, TValue> 
    : ValueListDictionaryBaseTestBase<TKey, TValue, IList<TValue>> where TKey : notnull
{
    protected override ValueListDictionaryBase<TKey, TValue, IList<TValue>> 
        ValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null)
    {
        return new ValueListDictionary<TKey, TValue>(comparer);
    }
    
    #region Constructors

    [Fact]
    public void Ctor_InitializesCorrectly()
    {
        var dict = new ValueListDictionary<TKey, TValue>();
        Assert.Equal(0, dict.Count);
        Assert.Equal(0, dict.KeyCount);
    }

    #endregion
}