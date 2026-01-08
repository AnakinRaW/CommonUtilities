using AnakinRaW.CommonUtilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
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

    protected sealed override ReadOnlyValueListDictionaryBase<TKey, TValue> ReadOnlyValueListDictionaryFactory(
        IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(dictionary);
    }

    protected override IValueListDictionary<TKey, TValue> MutableValueListDictionaryFactory()
    {
        return new FrugalValueListDictionary<TKey, TValue>();
    }

    protected override IEnumerable NonGenericIEnumerableFactory(int count)
    {
        var l = MutableValueListDictionaryFactory();
        AddToCollection(l, count);
        return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(l);
    }
    
    [Fact]
    public static void Empty_Idempotent()
    {
        Assert.NotNull(ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty);
        Assert.Equal(0, ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty.Count);
        Assert.Same(ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty, ReadOnlyFrugalValueListDictionary<TKey, TValue>.Empty);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetEnumerator(int count)
    {
        var l = MutableValueListDictionaryFactory();
        AddToCollection(l, count);

        var ro = new ReadOnlyFrugalValueListDictionary<TKey, TValue>(l);

        using var e1 = ro.GetEnumerator();
        using var e2 = ((IReadOnlyFrugalValueListDictionary<TKey, TValue>)ro).GetEnumerator();

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < count; j++)
            {
                Assert.True(e1.MoveNext());
                Assert.True(e2.MoveNext());

                _ = e1.Current;
                _ = e2.Current;
            }
            Assert.False(e1.MoveNext());
            Assert.False(e2.MoveNext());

            e1.Reset();
            e2.Reset();
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

public class ReadOnlyFrugalValueListDictionaryTest_FromNonFrugal : ReadOnlyFrugalValueListDictionaryTestBase<int, int>
{
    protected override bool DefaultValueAllowed => true;

    protected override IValueListDictionary<int, int> MutableValueListDictionaryFactory()
    {
        return new ValueListDictionary<int, int>();
    }

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