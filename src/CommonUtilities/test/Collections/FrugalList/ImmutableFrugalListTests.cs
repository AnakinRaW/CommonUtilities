using AnakinRaW.CommonUtilities.Collections;
using System;
using Xunit;
// ReSharper disable InconsistentNaming

namespace AnakinRaW.CommonUtilities.Test.Collections.FrugalList;

public class ImmutableFrugalListTestString : ImmutableFrugalListTestBase<string>
{
    protected override string CreateT(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes = new byte[stringLength];
        rand.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class ImmutableFrugalListTestInt : ImmutableFrugalListTestBase<int>
{
    private static readonly int[] _intArray = [-4, 5, -2, 3, 1, 2, -1, -3, 0, 4, -5, 3, 3];
    private static readonly int[] _excludedFromIntArray = [100, -34, 42, int.MaxValue, int.MinValue];

    protected override int CreateT(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }

    [Fact]
    public static void Contains()
    {
        var collection = ImmutableFrugalList.Create(_intArray);
        foreach (var item in _intArray)
            Assert.True(collection.Contains(item));

        foreach (var excluded in _excludedFromIntArray)
            Assert.False(collection.Contains(excluded));
    }

    [Fact]
    public static void IndexOf()
    {
        var collection = ImmutableFrugalList.Create(_intArray);

        foreach (var item in _intArray)
            Assert.Equal(Array.IndexOf(_intArray, item), collection.IndexOf(item));

        foreach (var excluded in _excludedFromIntArray)
            Assert.Equal(-1, collection.IndexOf(excluded));
    }
}