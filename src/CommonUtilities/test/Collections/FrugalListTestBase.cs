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

    #region Contains

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_ValidValueOnCollectionNotContainingThatValue(int count)
    {
        var collection = GenericICollectionFactory(count);
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
        var collection = GenericICollectionFactory(count);
        foreach (var item in collection)
            Assert.True(collection.Contains(item));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_DefaultValueOnCollectionNotContainingDefaultValue(int count)
    {
        var collection = GenericICollectionFactory(count);
        if (DefaultValueAllowed && default(T) is null) // it's true only for reference types and for Nullable<T>
        {
            Assert.False(collection.Contains(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Contains_DefaultValueOnCollectionContainingDefaultValue(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            collection.Add(default!);
            Assert.True(collection.Contains(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_ValidValueThatExistsTwiceInTheCollection(int count)
    {
        if (DuplicateValuesAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            var item = CreateT(12);
            collection.Add(item);
            collection.Add(item);
            Assert.Equal(count + 2, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Contains_InvalidValue_ThrowsArgumentException(int count)
    {
        var collection = GenericICollectionFactory(count);
        foreach (var value in InvalidValues)
        {
            Assert.Throws<ArgumentException>(() => collection.Contains(value));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Contains_DefaultValueWhenNotAllowed(int count)
    {
        if (!DefaultValueAllowed && !IsReadOnly)
        {
            var collection = GenericICollectionFactory(count);
            if (DefaultValueWhenNotAllowed_Throws)
                Assert.Throws<ArgumentNullException>(() => collection.Contains(default!));
            else
                Assert.False(collection.Contains(default!));
        }
    }

    #endregion

    #region IndexOf

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_DefaultValueNotContainedInList(int count)
    {
        if (DefaultValueAllowed)
        {
            var list = GenericIListFactory(count);
            var value = default(T);
            if (list.Contains(value!))
            {
                if (IsReadOnly)
                    return;
                list.Remove(value!);
            }
            Assert.Equal(-1, list.IndexOf(value!));
        }
        else
        {
            var list = GenericIListFactory(count);
            Assert.Throws<ArgumentNullException>(() => list.IndexOf(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_DefaultValueContainedInList(int count)
    {
        if (count > 0 && DefaultValueAllowed)
        {
            var list = GenericIListFactory(count);
            var value = default(T);
            if (!list.Contains(value!))
            {
                if (IsReadOnly)
                    return;
                list[0] = value!;
            }
            Assert.Equal(0, list.IndexOf(value!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_ValidValueNotContainedInList(int count)
    {
        var list = GenericIListFactory(count);
        var seed = 54321;
        var value = CreateT(seed++);
        while (list.Contains(value))
            value = CreateT(seed++);
        Assert.Equal(-1, list.IndexOf(value));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_ValueInCollectionMultipleTimes(int count)
    {
        if (count > 0 && !IsReadOnly && DuplicateValuesAllowed)
        {
            // IndexOf should always return the lowest index for which a matching element is found
            var list = GenericIListFactory(count);
            var value = CreateT(12345);
            list[0] = value;
            list[count / 2] = value;
            Assert.Equal(0, list.IndexOf(value));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_EachValueNoDuplicates(int count)
    {
        // Assumes no duplicate elements contained in the list returned by GenericIListFactory
        var list = GenericIListFactory(count);
        foreach (var i in Enumerable.Range(0, count))
        {
            Assert.Equal(i, list.IndexOf(list[i]));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_InvalidValue(int count)
    {
        if (!IsReadOnly)
        {
            foreach (var value in InvalidValues)
            {
                var list = GenericIListFactory(count);
                Assert.Throws<ArgumentException>(() => list.IndexOf(value));
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Generic_IndexOf_ReturnsFirstMatchingValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            foreach (var duplicate in list.ToList()) // hard copies list to circumvent enumeration error
                list.Add(duplicate);
            var expectedList = list.ToList();

            foreach (var i in Enumerable.Range(0, count))
            {
                Assert.Equal(i, list.IndexOf(expectedList[i]));
            }
        }
    }

    #endregion
}