using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using System;
using System.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

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

public abstract class ReadOnlyFrugalValueListDictionaryTestBase<TKey, TValue>
    : ReadOnlyValueListDictionaryBaseTestSuite<TKey, TValue>
    where TKey : notnull
{
    protected override bool DefaultValueAllowed => false;


    protected ReadOnlyFrugalValueListDictionary<TKey, TValue> ReadOnlyFrugalValueListDictionaryFactory(
        IReadOnlyFrugalValueListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(dictionary);
    }

    protected sealed override ReadOnlyValueListDictionaryBase<TKey, TValue> ReadOnlyValueListDictionaryFactory(
        IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is not IReadOnlyFrugalValueListDictionary<TKey, TValue> frugalValueList)
            throw new InvalidOperationException("invalid test construction");
        return ReadOnlyFrugalValueListDictionaryFactory(frugalValueList);
    }

    protected FrugalValueListDictionary<TKey, TValue> MutableFrugalValueListDictionaryFactory()
    {
        return new FrugalValueListDictionary<TKey, TValue>();
    }

    protected sealed override IValueListDictionary<TKey, TValue> MutableValueListDictionaryFactory()
    {
        return MutableFrugalValueListDictionaryFactory();
    }

    protected override IEnumerable NonGenericIEnumerableFactory(int count)
    {
        var l = MutableFrugalValueListDictionaryFactory();
        AddToCollection(l, count);
        return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(l);
    }

    #region Ctor

    [Fact]
    public void CtorTests_Negative()
    {
        AssertExtensions.Throws<ArgumentNullException>("dictionary",
            () => _ = new ReadOnlyFrugalValueListDictionary<TKey, TValue>(null!));
    }

    #endregion

    #region ReadOnlyFrugalValueListDictionary{TKey, TValue}.Empty

    [Fact]
    public static void Empty_Idempotent()
    {
        Assert.NotNull(ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty);
        Assert.Equal(0, ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty.Count);
        Assert.Same(ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty, ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty);
    }

    #endregion

    #region GetEnumerator

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetEnumerator(int count)
    {
        // ReSharper disable GenericEnumeratorNotDisposed

        var expectedDictionary = MutableFrugalValueListDictionaryFactory();
        AddToCollection(expectedDictionary, count);

        var ro = new ReadOnlyFrugalValueListDictionary<TKey, TValue>(expectedDictionary);

        // Verify struct enumerator can be obtained without using statement
        _ = ro.GetEnumerator();

        // IReadOnlyFrugalValueListDictionary enumerators
        AssertEnumeratorBehavior(
            count,
            ro.GetEnumerator(),
            expectedDictionary.GetEnumerator(),
            e => e.Current);

        AssertEnumeratorBehavior(
            count,
            ((IReadOnlyFrugalValueListDictionary<TKey, TValue>)ro).GetEnumerator(),
            ((IReadOnlyFrugalValueListDictionary<TKey, TValue>)expectedDictionary).GetEnumerator(),
            e => e.Current);

        // IReadOnlyValueListDictionary enumerator
        AssertEnumeratorBehavior(
            count,
            ((IReadOnlyValueListDictionary<TKey, TValue>)ro).GetEnumerator(),
            ((IReadOnlyValueListDictionary<TKey, TValue>)expectedDictionary).GetEnumerator(),
            e => e.Current);

        // IEnumerable enumerator
        AssertEnumeratorBehavior(
            count,
            ((IEnumerable)ro).GetEnumerator(),
            ((IEnumerable)expectedDictionary).GetEnumerator(),
            e => e.Current);

        // ReSharper restore GenericEnumeratorNotDisposed
    }

    #endregion

    #region Item Getter

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ReadOnlyFrugalValueListDictionary_ItemGet_DefaultKey(int count)
    {
        var collection = MutableFrugalValueListDictionaryFactory();
        AddToCollection(collection, count);
        var dictionary = ReadOnlyFrugalValueListDictionaryFactory(collection);

        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary[default!]);
            return;
        }

        var value = CreateTValue(3452);
        collection.Add(default!, value);
        Assert.Equal(value, dictionary[default!].First());
    }

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_ItemGet_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    var missingKey = GetNewKey(dictionary);
    //    Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_ItemGet_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    //{
    //    if (DefaultValueAllowed)
    //    {
    //        var dictionary = FrugalValueListDictionaryFactory(count);
    //        var missingKey = default(TKey)!;
    //        while (dictionary.ContainsKey(missingKey))
    //            dictionary.Remove(missingKey);
    //        Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
    //    }
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_ItemGet_PresentKeyReturnsCorrectValue(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    foreach (var pair in dictionary)
    //        Assert.Equal(pair.Value, dictionary[pair.Key]);
    //}

    #endregion

    #region GetValues

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_GetValues_DefaultKey(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    if (!DefaultValueAllowed)
    //    {
    //        Assert.Throws<ArgumentNullException>(() => dictionary.GetValues(default!));
    //        return;
    //    }

    //    var value = CreateTValue(3452);
    //    dictionary.Add(default!, value);
    //    Assert.Equal(value, dictionary.GetValues(default!).First());
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_GetValues_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    var missingKey = GetNewKey(dictionary);
    //    Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_GetValues_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    //{
    //    if (DefaultValueAllowed)
    //    {
    //        var dictionary = FrugalValueListDictionaryFactory(count);
    //        var missingKey = default(TKey)!;
    //        while (dictionary.ContainsKey(missingKey))
    //            dictionary.Remove(missingKey);
    //        Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
    //    }
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_GetValues_PresentKeyReturnsCorrectValue(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    foreach (var pair in dictionary)
    //        Assert.Equal(pair.Value, dictionary.GetValues(pair.Key));
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_GetValues_ReturnsSnapshot(int count)
    //{
    //    var dict = FrugalValueListDictionaryFactory(count);
    //    var key = GetNewKey(dict);
    //    var seed = 1234;
    //    dict.Add(key, CreateTValue(seed++));

    //    var values = dict.GetValues(key);

    //    var newValue = CreateTValue(seed);
    //    while (values.Contains(newValue))
    //        newValue = CreateTValue(++seed);

    //    dict.Add(key, newValue);

    //    // View reflects live changes, snapshot doesn't
    //    Assert.DoesNotContain(newValue, values);

    //    // After removal, neither view nor snapshot contains the value
    //    dict.Remove(key, newValue);
    //    Assert.DoesNotContain(newValue, values);

    //    // Removing key doesn't clear underlying list
    //    dict.Remove(key);
    //    Assert.NotEmpty(values);

    //    // Clearing dict doesn't clear underlying lists
    //    if (count > 0)
    //    {
    //        var firstKey = dict.Keys.First();
    //        var firstValues = dict.GetValues(firstKey);
    //        dict.Clear();
    //        Assert.NotEmpty(firstValues);
    //    }
    //}

    #endregion

    #region TryGetValues

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_TryGetValues_DefaultKey(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    if (!DefaultValueAllowed)
    //    {
    //        Assert.Throws<ArgumentNullException>(() => dictionary.TryGetValues(default!, out _));
    //        return;
    //    }

    //    var first = CreateTValue(3452);
    //    var second = CreateTValue(5431);
    //    dictionary.Add(default!, first);
    //    dictionary.Add(default!, second);
    //    Assert.True(dictionary.TryGetValues(default!, out var valueList));
    //    Assert.Equal([first, second], valueList);
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_TryGetValues_MissingNonDefaultKey_ReturnsFalseAndSetsDefault(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    var missingKey = GetNewKey(dictionary);
    //    Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
    //    Assert.Equal([], valueList);
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_TryGetValues_MissingDefaultKey_ReturnsFalseAndSetsDefault(int count)
    //{
    //    if (DefaultValueAllowed)
    //    {
    //        var dictionary = FrugalValueListDictionaryFactory(count);
    //        var missingKey = default(TKey)!;
    //        while (dictionary.ContainsKey(missingKey))
    //            dictionary.Remove(missingKey);
    //        Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
    //        Assert.Equal([], valueList);
    //    }
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void FrugalValueListDictionary_TryGetValues_PresentKeyReturnsCorrectValue(int count)
    //{
    //    var dictionary = FrugalValueListDictionaryFactory(count);
    //    foreach (var pair in dictionary)
    //    {
    //        Assert.True(dictionary.TryGetValues(pair.Key, out var valueList));
    //        Assert.Equal(pair.Value, valueList);
    //    }
    //}

    #endregion

    private static void AssertEnumeratorBehavior<TEnumerator, TCurrent>(
        int count,
        TEnumerator roEnumerator,
        TEnumerator expectedEnumerator,
        Func<TEnumerator, TCurrent> getCurrent)
        where TEnumerator : IEnumerator
    {
        try
        {
            for (var iteration = 0; iteration < 3; iteration++)
            {
                for (var j = 0; j < count; j++)
                {
                    Assert.True(expectedEnumerator.MoveNext());
                    Assert.True(roEnumerator.MoveNext());

                    var expectedCurrent = getCurrent(expectedEnumerator);
                    var roCurrent = getCurrent(roEnumerator);

                    Assert.Equal(expectedCurrent!.GetType(), roCurrent!.GetType());
                    Assert.Equal(expectedCurrent, roCurrent);
                }

                Assert.False(roEnumerator.MoveNext());
                Assert.False(expectedEnumerator.MoveNext());

                roEnumerator.Reset();
                expectedEnumerator.Reset();
            }
        }
        finally
        {
            (roEnumerator as IDisposable)?.Dispose();
            (expectedEnumerator as IDisposable)?.Dispose();
        }
    }
}

public class ReadOnlyFrugalValueListDictionaryTest_string_string 
    : ReadOnlyFrugalValueListDictionaryTestBase<string, string>
{
    protected override bool DefaultValueAllowed => false;

    protected override string CreateTKey(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes1 = new byte[stringLength];
        rand.NextBytes(bytes1);
        return Convert.ToBase64String(bytes1);
    }

    protected override string CreateTValue(int seed)
    {
        return CreateTKey(seed);
    }
}

public class ReadOnlyFrugalValueListDictionaryTest_int_int : ReadOnlyFrugalValueListDictionaryTestBase<int, int>
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