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

    #region Clear

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

    #endregion

    #region Add

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