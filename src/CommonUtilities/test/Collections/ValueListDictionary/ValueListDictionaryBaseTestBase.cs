using AnakinRaW.CommonUtilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ValueListDictionaryBaseTestBase<TKey, TValue, TList> : IValueListDictionaryTestBase<TKey, TValue>
    where TKey : notnull 
    where TList : IList<TValue>
{
    protected override KeyValuePair<TKey, IReadOnlyList<TValue>> CreateT(int seed)
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

    protected abstract ValueListDictionaryBase<TKey, TValue, TList> ValueListDictionaryFactory(
        IEqualityComparer<TKey>? comparer = null);

    protected virtual ValueListDictionaryBase<TKey, TValue, TList> ValueListDictionaryFactory(int count)
    {
        var collection = ValueListDictionaryFactory();
        AddToCollection(collection, count);
        return collection;
    }

    [Fact]
    public void Clear_OnEmptyCollection_DoesNotInvalidateEnumerator()
    {
        if (ModifyEnumeratorAllowed.HasFlag(ModifyOperation.Clear))
        {
            var dictionary = IValueListDictionaryFactory(0);
            IEnumerator valuesEnum = dictionary.GetEnumerator();

            dictionary.Clear();
            Assert.Empty(dictionary);
            Assert.False(valuesEnum.MoveNext());
        }
    }

    [Fact]
    public void Add_ItemAlreadyExists_DoesNotInvalidateEnumerator()
    {
        var dictionary = IValueListDictionaryFactory(0);
        var key = CreateTKey(123);
        var value = CreateTValue(123);
        
        dictionary.Add(key, value);

        IEnumerator valuesEnum = dictionary.GetEnumerator();
        dictionary.Add(key, value);

        Assert.True(valuesEnum.MoveNext());
    }

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


public abstract class Frugal<TKey, TValue> : ValueListDictionaryBaseTestBase<TKey, TValue, FrugalList<TValue>> 
    where TKey : notnull
{
  
    protected override ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>> 
        ValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null)
    {
        return new FrugalValueListDictionary<TKey, TValue>(comparer);
    }
}

public class IntFrugal : Frugal<int, int>
{
    protected override bool DefaultValueAllowed => true;

    protected override int CreateTKey(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }

    protected override int CreateTValue(int seed)
    {
        return CreateTKey(seed);
    }
}