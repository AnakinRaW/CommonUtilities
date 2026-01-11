using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.IList;

public abstract class ReadOnlyValueListDictionaryTestBase<TKey, TValue> 
    : ReadOnlyValueListDictionaryBaseTestSuite<TKey, TValue> 
    where TKey : notnull
{
    protected sealed override ReadOnlyValueListDictionaryBase<TKey, TValue> ReadOnlyValueListDictionaryFactory(
        IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyValueListDictionary<TKey, TValue>(dictionary);
    }

    [Fact]
    public void CtorTests_Negative()
    {
        AssertExtensions.Throws<ArgumentNullException>("dictionary",
            () => _ = new ReadOnlyValueListDictionary<TKey, TValue>(null!));
    }

    [Fact]
    public static void Empty_Idempotent()
    {
        Assert.NotNull(ReadOnlyValueListDictionary<TKey, TValue>.Empty);
        Assert.Equal(0, ReadOnlyValueListDictionary<TKey, TValue>.Empty.Count);
        Assert.Same(ReadOnlyValueListDictionary<TKey, TValue>.Empty, ReadOnlyValueListDictionary<TKey, TValue>.Empty);
    }
}