using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ReadOnlyValueListDictionaryTestBase<TKey, TValue> : IReadOnlyValueListDictionaryTestBase<TKey, TValue>
    where TKey : notnull
{
    protected override bool DefaultValueAllowed => false;

    protected override bool IsReadOnly => true;

    protected override KeyValuePair<TKey, IReadOnlyList<TValue>> CreateT(int seed)
    {
        throw new NotSupportedException();
    }

    protected virtual ReadOnlyValueListDictionary<TKey, TValue> ReadOnlyValueListDictionaryFactory(int count)
    {
        var collection = new ValueListDictionary<TKey, TValue>();
        AddToCollection(collection, count);
        return new ReadOnlyValueListDictionary<TKey, TValue>(collection);
    }

    protected override IReadOnlyValueListDictionary<TKey, TValue> IReadOnlyValueListDictionaryFactory(int count)
    {
        return ReadOnlyValueListDictionaryFactory(count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CtorTests(int count)
    {
        var collection = new ValueListDictionary<TKey, TValue>();
        AddToCollection(collection, count);
        var readOnlyDictionary = new ReadOnlyValueListDictionary<TKey, TValue>(collection);

        Assert.Equal(collection.KeyCount, readOnlyDictionary.KeyCount);
        Assert.Equal(collection.Count, readOnlyDictionary.Count);
    }

    [Fact]
    public static void CtorTests_Negative()
    {
        AssertExtensions.Throws<ArgumentNullException>("dictionary", () => _ = new ReadOnlyValueListDictionary<TKey, TValue>(null!));
    }

    [Fact]
    public static void Empty_Idempotent()
    {
        Assert.NotNull(ReadOnlyValueListDictionary<string, int>.Empty);
        Assert.Equal(0, ReadOnlyValueListDictionary<string, int>.Empty.Count);
        Assert.Same(ReadOnlyValueListDictionary<string, int>.Empty, ReadOnlyValueListDictionary<string, int>.Empty);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void EnumeratorTest(int count)
    {
        var collection = new ValueListDictionary<TKey, TValue>();
        AddToCollection(collection, count);
        var readOnlyDictionary = new ReadOnlyValueListDictionary<TKey, TValue>(collection);

        using var enumerator = readOnlyDictionary.GetEnumerator();
        foreach (var keyValuePair in collection)
        {
            Assert.True(enumerator.MoveNext());

            Assert.Equal(keyValuePair.Key, enumerator.Current.Key);
            Assert.Equal(keyValuePair.Value, enumerator.Current.Value);
        }
        Assert.False(enumerator.MoveNext());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void SourceModificationsReflectedInReadOnlyDictionary(int count)
    {
        var collection = new ValueListDictionary<TKey, TValue>();
        AddToCollection(collection, count);
        var readOnlyDictionary = new ReadOnlyValueListDictionary<TKey, TValue>(collection);

        Assert.Equal(collection.KeyCount, readOnlyDictionary.KeyCount);
        Assert.Equal(collection.Count, readOnlyDictionary.Count);

        collection.Add(GetNewKey(collection), CreateTValue(4231));

        Assert.Equal(collection.KeyCount, readOnlyDictionary.KeyCount);
        Assert.Equal(collection.Count, readOnlyDictionary.Count);
    }
}