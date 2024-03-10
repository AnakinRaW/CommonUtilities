using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections;

/// <summary>
/// Contains tests that ensure the correctness of the <see cref=" FrugalList{T}"/> class.
/// </summary>
public abstract class FrugalListTestBase<T> : IListTestSuite<T>
{
    protected override bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;

    protected override IList<T> GenericIListFactory()
    {
        return GenericFrugalListFactory();
    }

    protected override IList<T> GenericIListFactory(int count)
    {
        return GenericFrugalListFactory(count);
    }

    private static FrugalList<T> GenericFrugalListFactory()
    {
        return new FrugalList<T>();
    }

    private FrugalList<T> GenericFrugalListFactory(int count)
    {
        var toCreateFrom = CreateEnumerable(null, count, 0, 0);
        return new FrugalList<T>(toCreateFrom);
    }

    #region Ctors

    [Fact]
    public void Struct_Default()
    {
        var list = default(FrugalList<T>);
        Assert.Equal(0, list.Count);
        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void Constructor_Empty()
    {
        var list = new FrugalList<T>();
        Assert.Equal(0, list.Count);
        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void Constructor_Single()
    {
        var t = CreateT(0);
        var list = new FrugalList<T>(t);
        Assert.Equal(1, list.Count);
        Assert.Equal(t, list[0]);
        Assert.False(list.IsReadOnly);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Constructor_OtherFrugalList_Creates_Copy(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var source = GenericFrugalListFactory(count);
            ref var refSource = ref source;
            var other = new FrugalList<T>(in refSource);

            IList<T> asEnumerable = refSource;

            if (modifyEnumerable(asEnumerable))
                Assert.NotEqual(asEnumerable.ToList(), other.ToList());
        }
    }

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
    public void Constructor_IEnumerable(int _, int enumerableLength, int __, int numberOfDuplicateElements)
    {
        var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements);
        var list = new FrugalList<T>(enumerable);
        var expected = enumerable.ToList();

        Assert.Equal(enumerableLength, list.Count); //"Number of items in list do not match the number of items given."

        for (var i = 0; i < enumerableLength; i++)
            Assert.Equal(expected[i], list[i]); //"Expected object in item array to be the same as in the list"

        Assert.False(list.IsReadOnly); //"List should not be readonly"
    }

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
    public void Constructor_IEnumerable_Creates_Copy(int _, int enumerableLength, int __, int numberOfDuplicateElements)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements);
            var list = new FrugalList<T>(enumerable);

            if (modifyEnumerable(enumerable))
                Assert.NotEqual(enumerable.ToList(), list.ToList());
        }
    }

    [Fact]
    public void Constructor_NullIEnumerable_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new FrugalList<T>(null!); });
    }

    #endregion

    #region Boxing & ByRef Behavior

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Boxing_ReflectsAllChanges(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var source = GenericIEnumerableFactory(count);
            var copy = source;

            if (modifyEnumerable(source))
                Assert.Equal(source.ToList(), copy.ToList());
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ByRef_ReflectsAllChanges(int count)
    {
        var source = GenericFrugalListFactory(count);

        ref var copy = ref source;

        copy.Add(CreateT(0));
        copy.Insert(0, CreateT(1));

        Assert.Equal(source.ToList(), copy.ToList());
    }

    #endregion

    #region CopyByValue Behavior

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_Clear(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;

        source.Clear();
        if (count >= 1)
            Assert.NotEqual(source.ToList(), copy.ToList());
        else
            Assert.Equal(source.ToList(), copy.ToList()); // Clear on empty list does not have visible changes
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_AddingItems(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;

        source.Add(CreateT(0));
        if (count <= 1)
            Assert.NotEqual(source.ToList(), copy.ToList());
        else
            Assert.Equal(source.ToList(), copy.ToList()); // Only adding items to the backing lists gets reflected
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_InsertFirst(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;

        source.Insert(0, CreateT(0));
        Assert.NotEqual(source.ToList(), copy.ToList());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_RemoveFirst(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;
        if (count <= 0)
            return;

        source.RemoveAt(0);
        Assert.NotEqual(source.ToList(), copy.ToList());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_RemoveLast(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;
        if (count <= 0)
            return;

        source.RemoveAt(count - 1);
        if (count == 1)
            Assert.NotEqual(source.ToList(), copy.ToList());
        else
            Assert.Equal(source.ToList(), copy.ToList()); // Only removing items from the backing lists gets reflected
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_OverrideFirst(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;
        if (count <= 0)
            return;

        source[0] = CreateT(0);
        Assert.NotEqual(source.ToList(), copy.ToList());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void CopyByValue_SideEffects_OverrideLast(int count)
    {
        var source = GenericFrugalListFactory(count);
        var copy = source;
        if (count <= 0)
            return;

        source[count - 1] = CreateT(0);
        if (count == 1)
            Assert.NotEqual(source.ToList(), copy.ToList());
        else
            Assert.Equal(source.ToList(), copy.ToList()); // Only modifying items from the backing lists gets reflected
    }

    #endregion

    #region Linq Equivalents

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ToList(int count)
    {
        var enumerable = CreateEnumerable(null, count, 0, 0);
        var list = new FrugalList<T>(enumerable);
        Assert.Equal(enumerable.ToList(), list.ToList());
    }


    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void FirstOrDefault(int count)
    {
        var list = GenericFrugalListFactory(count);
        Assert.Equal(count == 0 ? default : list[0], list.FirstOrDefault());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void First(int count)
    {
        var list = GenericFrugalListFactory(count);
        if (count == 0)
            Assert.Throws<InvalidOperationException>(() => list.First());
        else
            Assert.Equal(list[0], list.First());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void LastOrDefault(int count)
    {
        var list = GenericFrugalListFactory(count);
        Assert.Equal(count == 0 ? default : list[count - 1], list.LastOrDefault());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Last(int count)
    {
        var list = GenericFrugalListFactory(count);
        if (count == 0)
            Assert.Throws<InvalidOperationException>(() => list.Last());
        else
            Assert.Equal(list[count - 1], list.Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ToArray(int count)
    {
        var list = GenericFrugalListFactory(count);
        var array = list.ToArray();
        Assert.Equal(list.ToList(), array);
    }

    #endregion

    #region Get Enumerator

    [Theory]
    [MemberData(nameof(GetEnumerableTestData))]
    public void GetEnumerator(int _, int enumerableLength, int __, int numberOfDuplicateElements)
    {
        var enumerable = CreateEnumerable(null, enumerableLength, 0, numberOfDuplicateElements);
        var list = new FrugalList<T>(enumerable);

        var actualList = new List<T>();

        using var enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
            actualList.Add(enumerator.Current);

        Assert.Equal(enumerable.ToList(), actualList);
    }

    #endregion
}