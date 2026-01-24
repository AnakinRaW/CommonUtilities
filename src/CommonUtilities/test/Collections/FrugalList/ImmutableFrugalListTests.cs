using AnakinRaW.CommonUtilities.Collections;
using System;
using System.Linq;
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
#pragma warning disable xUnit2017
        var collection = ImmutableFrugalList.Create(_intArray);
        foreach (var item in _intArray)
            Assert.True(collection.Contains(item));

        foreach (var excluded in _excludedFromIntArray)
            Assert.False(collection.Contains(excluded));
#pragma warning restore xUnit2017
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

public class ImmutableFrugalListTest
{
    #region Create{T}

    [Fact]
    public void Create_NullArg_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("items", () => ImmutableFrugalList.Create<int>(null!));
    }

    [Fact]
#pragma warning disable xUnit1026
    public void Create_CreatesCorrectImmutableList()
    {
        var list = new[] { 1, 2, 3, 4, 5, 6 };

        var listAsFrugal = new FrugalList<int>(list);
        var listAsImmutable = listAsFrugal.ToImmutableList();
        var listAsSet = list.ToHashSet();
        var listAsEnumerable = list.Where(_ => true);

        Assert.Equal(list, ImmutableFrugalList.Create(listAsFrugal));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsImmutable));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsSet));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsEnumerable));
    }
#pragma warning restore xUnit1026

    #endregion

    #region Single{T}

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData("123")]
    public void Single_CreatesCorrectImmutableListWithOneItem(object? data)
    {
        var list = ImmutableFrugalList.Single(data);
        Assert.Equal([data], list);
        Assert.Single(list);
    }

    #endregion
}