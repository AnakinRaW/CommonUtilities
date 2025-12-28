using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ValueListDictionaryTestBase<TKey, TValue> : IValueListDictionaryTestBase<TKey, TValue> where TKey : notnull
{
    protected override bool DefaultValueAllowed => false;

    protected override KeyValuePair<TKey, ReadOnlyFrugalList<TValue>> CreateT(int seed)
    {
        throw new NotSupportedException();
    }

    protected override IValueListDictionary<TKey, TValue> IValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null)
    {
        return ValueListDictionaryFactory(comparer);
    }

    protected override IValueListDictionary<TKey, TValue> IValueListDictionaryFactory(int count)
    {
        return ValueListDictionaryFactory(count);
    }

    protected ValueListDictionary<TKey, TValue> ValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null)
    {
        return new ValueListDictionary<TKey, TValue>(comparer);
    }

    protected virtual ValueListDictionary<TKey, TValue> ValueListDictionaryFactory(int count)
    {
        var collection = ValueListDictionaryFactory();
        AddToCollection(collection, count);
        return collection;
    }

    #region Constructors

    [Fact]
    public void Dictionary_CapacityAtLeastPassedValue()
    {
        var dict = new ValueListDictionary<TKey, TValue>();
        Assert.Equal(0, dict.Count);
        Assert.Equal(0, dict.KeyCount);
    }

    #endregion

    #region IReadOnlyValueListDictionary<TKey, TValue>.Keys & Values

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IReadOnlyValueListDictionary_Keys_ContainsAllCorrectKeys(int count)
    {
        var dictionary = ValueListDictionaryFactory(count);
        var expected = dictionary.Select(pair => pair.Key);
        IEnumerable<TKey> keys = ((IReadOnlyValueListDictionary<TKey, TValue>)dictionary).Keys;
        Assert.True(expected.SequenceEqual(keys));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IReadOnlyValueListDictionary_Values_ContainsAllCorrectValues(int count)
    {
        var dictionary = ValueListDictionaryFactory(count);
        var expected = dictionary.SelectMany(pair => pair.Value);
        IEnumerable<TValue> values = ((IReadOnlyValueListDictionary<TKey, TValue>)dictionary).Values;
        Assert.True(expected.SequenceEqual(values));
    }

    #endregion
}