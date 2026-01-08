using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.Frugal;

public abstract class FrugalValueListDictionaryTestBase<TKey, TValue> 
    : ValueListDictionaryBaseTestSuite<TKey, TValue, FrugalList<TValue>> 
    where TKey : notnull
{
    protected override bool ValueList_IsReadOnlyView => false;

    protected FrugalValueListDictionary<TKey, TValue> FrugalValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null)
    {
        return new FrugalValueListDictionary<TKey, TValue>(comparer);
    }

    protected FrugalValueListDictionary<TKey, TValue> FrugalValueListDictionaryFactory(int count)
    {
        var dict = FrugalValueListDictionaryFactory();
        AddToCollection(dict, count);
        return dict;
    }

    protected override ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>> ValueListDictionaryFactory(int count)
    {
        return FrugalValueListDictionaryFactory(count);
    }

    protected override ValueListDictionaryBase<TKey, TValue, FrugalList<TValue>> ValueListDictionaryFactory(
        IEqualityComparer<TKey>? comparer = null)
    {
        return FrugalValueListDictionaryFactory();
    }

    #region Constructors

    [Fact]
    public void Ctor_InitializesCorrectly()
    {
        var dict = new FrugalValueListDictionary<TKey, TValue>();
        Assert.Equal(0, dict.Count);
        Assert.Equal(0, dict.KeyCount);
    }

    #endregion

    #region GetEnumerator

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetEnumerator_EnumeratesDictionaryCorrectly(int count)
    {
        var collection = FrugalValueListDictionaryFactory(count);
        
        using var enumerator1 = collection.GetEnumerator();
        using var enumerator2 = ((IReadOnlyFrugalValueListDictionary<TKey, TValue>)collection).GetEnumerator();
        IEnumerator enumerator3 = collection.GetEnumerator();

        foreach (var keyValuePair in collection)
        {
            Assert.True(enumerator1.MoveNext());
            Assert.True(enumerator2.MoveNext());
            Assert.True(enumerator3.MoveNext());

            Assert.Equal(keyValuePair.Key, enumerator1.Current.Key);
            Assert.Equal(keyValuePair.Key, enumerator2.Current.Key);
            Assert.Equal(keyValuePair.Key, ((KeyValuePair<TKey, ImmutableFrugalList<TValue>>)enumerator3.Current).Key);

            Assert.Equal(keyValuePair.Value, enumerator1.Current.Value);
            Assert.Equal(keyValuePair.Value, enumerator2.Current.Value);
            Assert.Equal(keyValuePair.Value, ((KeyValuePair<TKey, ImmutableFrugalList<TValue>>)enumerator3.Current).Value);
        }
        Assert.False(enumerator1.MoveNext());

        if (enumerator3 is IDisposable disposable)
            disposable.Dispose();
    }

    #endregion

    #region Item Getter

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_ItemGet_DefaultKey(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary[default!]);
            return;
        }

        var value = CreateTValue(3452);
        dictionary.Add(default!, value);
        Assert.Equal(value, dictionary[default!].First());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_ItemGet_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_ItemGet_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = FrugalValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                dictionary.Remove(missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_ItemGet_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value, dictionary[pair.Key]);
    }

    #endregion

    #region GetValues

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_GetValues_DefaultKey(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.GetValues(default!));
            return;
        }

        var value = CreateTValue(3452);
        dictionary.Add(default!, value);
        Assert.Equal(value, dictionary.GetValues(default!).First());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_GetValues_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_GetValues_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = FrugalValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                dictionary.Remove(missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_GetValues_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value, dictionary.GetValues(pair.Key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_GetValues_ReturnsSnapshot(int count)
    {
        var dict = FrugalValueListDictionaryFactory(count);
        var key = GetNewKey(dict);
        var seed = 1234;
        dict.Add(key, CreateTValue(seed++));

        var values = dict.GetValues(key);

        var newValue = CreateTValue(seed);
        while (values.Contains(newValue))
            newValue = CreateTValue(++seed);

        dict.Add(key, newValue);

        // View reflects live changes, snapshot doesn't
        Assert.DoesNotContain(newValue, values);

        // After removal, neither view nor snapshot contains the value
        dict.Remove(key, newValue);
        Assert.DoesNotContain(newValue, values);

        // Removing key doesn't clear underlying list
        dict.Remove(key);
        Assert.NotEmpty(values);

        // Clearing dict doesn't clear underlying lists
        if (count > 0)
        {
            var firstKey = dict.Keys.First();
            var firstValues = dict.GetValues(firstKey);
            dict.Clear();
            Assert.NotEmpty(firstValues);
        }
    }

    #endregion

    #region TryGetValues

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_TryGetValues_DefaultKey(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.TryGetValues(default!, out _));
            return;
        }

        var first = CreateTValue(3452);
        var second = CreateTValue(5431);
        dictionary.Add(default!, first);
        dictionary.Add(default!, second);
        Assert.True(dictionary.TryGetValues(default!, out var valueList));
        Assert.Equal([first, second], valueList);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_TryGetValues_MissingNonDefaultKey_ReturnsFalseAndSetsDefault(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
        Assert.Equal([], valueList);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_TryGetValues_MissingDefaultKey_ReturnsFalseAndSetsDefault(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = FrugalValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                dictionary.Remove(missingKey);
            Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
            Assert.Equal([], valueList);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FrugalValueListDictionary_TryGetValues_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = FrugalValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
        {
            Assert.True(dictionary.TryGetValues(pair.Key, out var valueList));
            Assert.Equal(pair.Value, valueList);
        }
    }

    #endregion
}