using System;
using AnakinRaW.CommonUtilities.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ReadOnlyValueListDictionaryBaseTestSuite<TKey, TValue> : IReadOnlyValueListDictionaryTestBase<TKey, TValue>
    where TKey : notnull
{
    protected override bool DefaultValueAllowed => false;

    protected override bool IsReadOnly => true;

    protected virtual IValueListDictionary<TKey, TValue> MutableValueListDictionaryFactory()
    {
        return new ValueListDictionary<TKey, TValue>();
    }

    protected abstract ReadOnlyValueListDictionaryBase<TKey, TValue> ReadOnlyValueListDictionaryFactory(
        IReadOnlyValueListDictionary<TKey, TValue> dictionary);

    protected ReadOnlyValueListDictionaryBase<TKey, TValue> ReadOnlyValueListDictionaryFactory(int count)
    {
        var collection = MutableValueListDictionaryFactory();
        AddToCollection(collection, count);
        return ReadOnlyValueListDictionaryFactory(collection);
    }

    protected sealed override IReadOnlyValueListDictionary<TKey, TValue> IReadOnlyValueListDictionaryFactory(int count)
    {
        return ReadOnlyValueListDictionaryFactory(count);
    }

    protected override IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>> GenericIEnumerableFactory(
        int count)
    {
        var collection = MutableValueListDictionaryFactory();
        AddToCollection(collection, count);
        return ReadOnlyValueListDictionaryFactory(collection);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CtorTests(int count)
    {
        var collection = MutableValueListDictionaryFactory();
        AddToCollection(collection, count);
        var readOnlyDictionary = ReadOnlyValueListDictionaryFactory(collection);

        Assert.Equal(collection.Count, readOnlyDictionary.Count);
        Assert.Equal(collection.ValueCount, readOnlyDictionary.ValueCount);

        VerifyReadOnlyValueListDictionary(readOnlyDictionary, collection);
        VerifyReadOnlyValueListDictionary(ReadOnlyValueListDictionaryFactory(readOnlyDictionary), collection);
    }
    
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void SourceModificationsReflectedInReadOnlyDictionary(int count)
    {
        var collection = MutableValueListDictionaryFactory();
        AddToCollection(collection, count);
        var readOnlyDictionary = ReadOnlyValueListDictionaryFactory(collection);

        Assert.Equal(collection.Count, readOnlyDictionary.Count);
        Assert.Equal(collection.ValueCount, readOnlyDictionary.ValueCount);

        collection.Add(GetNewKey(collection), CreateTValue(4231));

        Assert.Equal(collection.Count, readOnlyDictionary.Count);
        Assert.Equal(collection.ValueCount, readOnlyDictionary.ValueCount);
    }

    private static void VerifyReadOnlyValueListDictionary(
        ReadOnlyValueListDictionaryBase<TKey, TValue> readOnlyDictionary,
        IValueListDictionary<TKey, TValue> expectedDict)
    {
        Assert.Equal(expectedDict.ValueCount, readOnlyDictionary.ValueCount);
        foreach (var key in expectedDict.Keys)
        {
            var expectedValue = expectedDict[key];
            Assert.Equal(expectedValue, readOnlyDictionary[key]);
        }
        VerifyGenericEnumerator(readOnlyDictionary, expectedDict);

        VerifyEnumerator(readOnlyDictionary, expectedDict);
    }

    private static void VerifyGenericEnumerator(
        ReadOnlyValueListDictionaryBase<TKey, TValue> readOnlyDictionary,
        IValueListDictionary<TKey, TValue> expectedDict)
    {
        var enumerator = readOnlyDictionary.GetEnumerator();
        var iterations = 0;
        var expectedCount = expectedDict.Count;

        var keys = expectedDict.Keys.ToList();

        while (iterations < expectedCount && enumerator.MoveNext())
        {
            var currentItem = enumerator.Current;

            // Verify we have not gotten more items then we expected
            Assert.True(iterations < expectedCount,
                "More items have been returned from the enumerator(" + iterations + " items) " +
                "then are in the expectedElements(" + expectedCount + " items)");

            var expectedKey = keys[iterations];

            Assert.Equal(expectedKey, currentItem.Key);
            Assert.Equal(expectedDict[expectedKey], currentItem.Value);
            
            // Verify Current always returns the same value every time it is called
            for (var i = 0; i < 3; i++)
            {
                var tempItem = enumerator.Current;
                Assert.Equal(currentItem, tempItem);
            }

            iterations++;
        }

        Assert.Equal(expectedCount, iterations);

        for (var i = 0; i < 3; i++)
        {
            Assert.False(enumerator.MoveNext(), 
                "Expected MoveNext to return false after" + iterations + " iterations");
        }

        enumerator.Dispose();
    }

    private static void VerifyEnumerator(
        ReadOnlyValueListDictionaryBase<TKey, TValue> readOnlyDictionary,
        IValueListDictionary<TKey, TValue> expectedDict)
    {
        IEnumerator enumerator = readOnlyDictionary.GetEnumerator();
        var iterations = 0;
        var expectedCount = expectedDict.Count;

        var keys = expectedDict.Keys.ToList();

        while ((iterations < expectedCount) && enumerator.MoveNext())
        {
            var currentItem = (KeyValuePair<TKey, IReadOnlyList<TValue>>) enumerator.Current;

            // Verify we have not gotten more items then we expected
            Assert.True(iterations < expectedCount,
                "More items have been returned from the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

            var expectedKey = keys[iterations];

            Assert.Equal(expectedKey, currentItem.Key);
            Assert.Equal(expectedDict[expectedKey], currentItem.Value);

            // Verify Current always returns the same value every time it is called
            for (var i = 0; i < 3; i++)
            {
                var tempItem = enumerator.Current;
                Assert.Equal(currentItem, tempItem);
            }

            iterations++;
        }

        Assert.Equal(expectedCount, iterations);

        for (var i = 0; i < 3; i++)
        {
            Assert.False(enumerator.MoveNext(), "Expected MoveNext to return false after" + iterations + " iterations");
        }
        
        if (enumerator is IDisposable disposable)
            disposable.Dispose();
    }

}