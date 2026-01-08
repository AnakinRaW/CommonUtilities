using AnakinRaW.CommonUtilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.FrugalList;

/// <summary>
/// Contains tests that ensure the correctness of the <see cref="ImmutableFrugalList{T}"/> class.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class ImmutableFrugalListTestBase<T> : FrugalListTestSuite<T>
{
    protected override bool IsReadOnly => true;

    /// <inheritdoc />
    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        yield break;
    }

    protected virtual ImmutableFrugalList<T> GenericReadOnlyListFrugalListFactory(IEnumerable<T> enumerable)
    {
        return ImmutableFrugalList.Create(enumerable);
    }

    protected virtual ImmutableFrugalList<T> GenericReadOnlyListFrugalListFactory(int count)
    {
        var baseCollection = CreateEnumerable(null, count, 0, 0);
        return GenericReadOnlyListFrugalListFactory(baseCollection);
    }

    protected override IList<T> GenericIListFactory()
    {
        return GenericReadOnlyListFrugalListFactory(0);
    }

    protected override IList<T> GenericIListFactory(int count)
    {
        return GenericReadOnlyListFrugalListFactory(count);
    }

    #region ICollection{T}.IsReadOnly

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IsReadOnly_ReturnsTrue(int count)
    {
        ICollection<T> list = GenericReadOnlyListFrugalListFactory(count);
        Assert.True(list.IsReadOnly);
    }

    #endregion

    #region Create{T}

    [Fact]
    public void Create_NullArg_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("items", () => ImmutableFrugalList.Create<T>(null!));
    }

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
    public void Create_CreatesCorrectImmutableList(int _, int enumerableLength, int __, int ___)
    {
        var list = CreateEnumerable(null, enumerableLength, 0, 0).ToList();

        var listAsFrugal = new FrugalList<T>(list);
        var listAsImmutable = listAsFrugal.ToImmutableList();
        var listAsSet = list.ToHashSet();
        var listAsEnumerable = list.Where(_ => true);

        Assert.Equal(list, ImmutableFrugalList.Create(listAsFrugal));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsImmutable));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsSet));
        Assert.Equal(list, ImmutableFrugalList.Create(listAsEnumerable));

        var mods = ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

        foreach (var modifyEnumerable in GetModifyEnumerables(mods, CreateT))
        {
            var listCopy = new List<T>(list);
            var immutable = ImmutableFrugalList.Create(listCopy);
            if (modifyEnumerable(listCopy))
                Assert.NotEqual(listCopy, immutable.ToList());
        }
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_Idempotent()
    {
 #pragma warning disable xUnit2002
        Assert.NotNull(ImmutableFrugalList<T>.Empty);
 #pragma warning restore xUnit2002
#pragma warning disable xUnit2013
        Assert.Equal(0, ImmutableFrugalList<T>.Empty.Count);
#pragma warning restore xUnit2013
        Assert.Equal(ImmutableFrugalList<T>.Empty, ImmutableFrugalList<T>.Empty);
    }

    #endregion

    #region Ctors

    [Fact]
    public void Ctor_Single()
    {
        var t = CreateT(0);
        // ReSharper disable once CollectionNeverUpdated.Local
        var list = new ImmutableFrugalList<T>(t);
#pragma warning disable xUnit2013
        Assert.Equal(1, list.Count);
#pragma warning restore xUnit2013
        Assert.Equal(t, list[0]);
    }

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
    public void Ctor_FrugalListIn(int _, int enumerableLength, int __, int numberOfDuplicateElements)
    {
        var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements);
        var frugal = new FrugalList<T>(enumerable);
        var immutable = new ImmutableFrugalList<T>(in frugal);

        Assert.Equal(frugal, immutable);
    }

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
 #pragma warning disable xUnit1026
    public void Ctor_ModificationsGetNotReflectedWhenOriginalListChanges(int _, int enumerableLength, int __, int numberOfDuplicateElements)
 #pragma warning restore xUnit1026
    {
        var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements);

        var frugal = new FrugalList<T>(enumerable);
        ref var refFrugal = ref frugal;

        var immutable = new ImmutableFrugalList<T>(in frugal);

        Assert.Equal(refFrugal.ToList(), immutable.ToList());

        if (enumerableLength == 0)
            return;

        var asEnumerable = (IList<T>)frugal;

        var mods = ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

        foreach (var modifyEnumerable in GetModifyEnumerables(mods, CreateT))
        {
            var listCopy = new List<T>(asEnumerable);
            if (modifyEnumerable(listCopy))
                Assert.NotEqual(listCopy, immutable.ToList());
        }
    }

    #endregion

    #region Copy To

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_NullArray_ThrowsArgumentNullException(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var array = new T[count];
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, int.MinValue));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var array = new T[count];
        if (count > 0)
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, count));
        else
            collection.CopyTo(array, count); // does nothing since the array is empty
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var array = new T[count];
        Assert.Throws(ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType, () => collection.CopyTo(array, count + 1));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
    {
        if (count > 0) // Want the T array to have at least 1 element
        {
            var collection = GenericReadOnlyListFrugalListFactory(count);
            var array = new T[count];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 1));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_ExactlyEnoughSpaceInArray(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var array = new T[count];
        collection.CopyTo(array, 0);
        Assert.True(collection.SequenceEqual(array));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyTo_ArrayIsLargerThanCollection(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var array = new T[count * 3 / 2];
        collection.CopyTo(array, 0);
        Assert.True(collection.SequenceEqual(array.Take(count)));
    }

    #endregion

    #region Contains
    
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_ValidValueOnCollectionNotContainingThatValue(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        var seed = 4315;
        var item = CreateT(seed++);
        while (collection.Contains(item))
            item = CreateT(seed++);
        Assert.False(collection.Contains(item));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_ValidValueOnCollectionContainingThatValue(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        foreach (var item in collection)
            Assert.True(collection.Contains(item));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_DefaultValueOnCollectionNotContainingDefaultValue(int count)
    {
        var collection = GenericReadOnlyListFrugalListFactory(count);
        if (default(T) is null)
            Assert.False(collection.Contains(default!));
    }

    #endregion

    #region IndexOf

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void IList_Generic_IndexOf_DefaultValueNotContainedInList(int count)
    //{
    //    var list = GenericReadOnlyListFrugalListFactory(count);
    //    var value = default(T);
    //    if (list.Contains(value!))
    //        return;
    //    Assert.Equal(-1, list.IndexOf(value!));
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void IList_Generic_IndexOf_DefaultValueContainedInList(int count)
    //{
    //    if (count > 0)
    //    {
    //        var list = GenericReadOnlyListFrugalListFactory(count);
    //        var value = default(T);
    //        if (!list.Contains(value!))
    //            return;
    //        Assert.Equal(0, list.IndexOf(value!));
    //    }
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void IList_Generic_IndexOf_ValidValueNotContainedInList(int count)
    //{
    //    var list = GenericReadOnlyListFrugalListFactory(count);
    //    var seed = 54321;
    //    var value = CreateT(seed++);
    //    while (list.Contains(value))
    //        value = CreateT(seed++);
    //    Assert.Equal(-1, list.IndexOf(value));
    //}

    //[Theory]
    //[MemberData(nameof(ValidCollectionSizes))]
    //public void IList_Generic_IndexOf_EachValueNoDuplicates(int count)
    //{
    //    // Assumes no duplicate elements contained in the list returned by GenericIListFactory
    //    var list = GenericReadOnlyListFrugalListFactory(count);
    //    foreach (var i in Enumerable.Range(0, count)) 
    //        Assert.Equal(i, list.IndexOf(list[i]));
    //}

    #endregion

    #region Linq Equivalents

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ToList(int count)
    {
        var enumerable = CreateEnumerable(null, count, 0, 0).ToList();
        var list = new FrugalList<T>(enumerable);
        Assert.Equal(enumerable.ToList(), list.ToList());
    }


    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FirstOrDefault(int count)
    {
        var list = GenericReadOnlyListFrugalListFactory(count);
        Assert.Equal(count == 0 ? default : list[0], list.FirstOrDefault());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void First(int count)
    {
        var list = GenericReadOnlyListFrugalListFactory(count);
        if (count == 0)
            Assert.Throws<InvalidOperationException>(() => list.First());
        else
            Assert.Equal(list[0], list.First());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void LastOrDefault(int count)
    {
        var list = GenericReadOnlyListFrugalListFactory(count);
        Assert.Equal(count == 0 ? default : list[count - 1], list.LastOrDefault());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Last(int count)
    {
        var list = GenericReadOnlyListFrugalListFactory(count);
        if (count == 0)
            Assert.Throws<InvalidOperationException>(() => list.Last());
        else
            Assert.Equal(list[count - 1], list.Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ToArray(int count)
    {
        var list = GenericReadOnlyListFrugalListFactory(count);
        var array = list.ToArray();
        Assert.Equal(list.ToList(), array);
    }

    #endregion

    #region Get Enumerator

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
 #pragma warning disable xUnit1026
    public void GetEnumerator(int _, int enumerableLength, int __, int numberOfDuplicateElements)
 #pragma warning restore xUnit1026
    {
        var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements).ToList();
        var list = new FrugalList<T>(enumerable);

        var actualList = new List<T>();

        using var enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
            actualList.Add(enumerator.Current);

        Assert.Equal(enumerable.ToList(), actualList);
    }

    #endregion
}