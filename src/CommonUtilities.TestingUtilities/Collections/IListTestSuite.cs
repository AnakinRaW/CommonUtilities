using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AnakinRaW.CommonUtilities.Testing.Collections;

// This test suite is taken from the .NET runtime repository (https://github.com/dotnet/runtime) and adapted to the VSTesting Framework.
// The .NET Foundation licenses this under the MIT license.
/// <summary>
/// Contains tests that ensure the correctness of any class that implements the generic
/// <see cref="IList{T}"/> interface
/// </summary>
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class IListTestSuite<T> : ICollectionTestSuite<T> 
{
    protected virtual Type IList_Generic_Item_InvalidIndex_ThrowType => typeof(ArgumentOutOfRangeException);

    /// <summary>
    /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
    /// </summary>
    public static IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations, Func<int, T> createT)
    {
        return new ModifyEnumerableList<T>(createT).GetModifyEnumerables(operations);
    }

    /// <summary>
    /// Creates an instance of an <see cref="IList{T}"/> that can be used for testing.
    /// </summary>
    /// <returns>An instance of an <see cref="IList{T}"/> that can be used for testing.</returns>
    protected abstract IList<T> GenericIListFactory();

    /// <summary>
    /// Creates an instance of an <see cref="IList{T}"/> that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned <see cref="IList{T}"/> contains.</param>
    /// <returns>An instance of an <see cref="IList{T}"/> that can be used for testing.</returns>
    protected virtual IList<T> GenericIListFactory(int count)
    {
        var collection = GenericIListFactory();
        AddToCollection(collection, count);
        return collection;
    }

    /// <summary>
    /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
    /// </summary>
    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        foreach (var item in base.GetModifyEnumerables(operations))
            yield return item;

        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Insert))
        {
            yield return enumerable =>
            {
                var casted = (IList<T>)enumerable;
                if (casted.Count > 0)
                {
                    casted.Insert(0, CreateT(12));
                    return true;
                }
                return false;
            };
        }
        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Overwrite))
        {
            yield return enumerable =>
            {
                var casted = (IList<T>)enumerable;
                if (casted.Count > 0)
                {
                    casted[0] = CreateT(12);
                    return true;
                }
                return false;
            };
        }
        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Remove))
        {
            yield return enumerable =>
            {
                var casted = (IList<T>)enumerable;
                if (casted.Count > 0)
                {
                    casted.RemoveAt(0);
                    return true;
                }
                return false;
            };
        }
    }

    protected override ICollection<T> GenericICollectionFactory()
    {
        return GenericIListFactory();
    }

    protected override ICollection<T> GenericICollectionFactory(int count)
    {
        return GenericIListFactory(count);
    }

    #region Item Getter

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemGet_NegativeIndex_ThrowsException(int count)
    {
        var list = GenericIListFactory(count);
       Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[-1]);
        Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[int.MinValue]);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemGet_IndexGreaterThanListCount_ThrowsException(int count)
    {
        var list = GenericIListFactory(count);
        Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count]);
        Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count + 1]);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemGet_ValidGetWithinListBounds(int count)
    {
        var list = GenericIListFactory(count);

        foreach (var i in Enumerable.Range(0, count)) 
            Sink(list[i]);
        return;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void Sink(T _) { }
    }

    #endregion

    #region Item Setter

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_NegativeIndex_ThrowsException(int count)
    {
        if (!IsReadOnly)
        {
            var list = GenericIListFactory(count);
            var validAdd = CreateT(0);
            Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[-1] = validAdd);
            Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[int.MinValue] = validAdd);
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_IndexGreaterThanListCount_ThrowsException(int count)
    {
        if (!IsReadOnly)
        {
            var list = GenericIListFactory(count);
            var validAdd = CreateT(0);
            Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count] = validAdd);
            Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count + 1] = validAdd);
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_OnReadOnlyList(int count)
    {
        if (IsReadOnly && count > 0)
        {
            var list = GenericIListFactory(count);
            var before = list[count / 2];
            Assert.Throws<NotSupportedException>(() => list[count / 2] = CreateT(321432));
            Assert.Equal(before, list[count / 2]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_FirstItemToNonDefaultValue(int count)
    {
        if (count > 0 && !IsReadOnly)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            list[0] = value;
            Assert.Equal(value, list[0]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_FirstItemToDefaultValue(int count)
    {
        if (count > 0 && !IsReadOnly)
        {
            var list = GenericIListFactory(count);
            if (DefaultValueAllowed)
            {
                list[0] = default!;
                Assert.Equal(default, list[0]);
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => list[0] = default!);
                Assert.NotEqual(default, list[0]);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_LastItemToNonDefaultValue(int count)
    {
        if (count > 0 && !IsReadOnly)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            var lastIndex = count - 1;
            list[lastIndex] = value;
            Assert.Equal(value, list[lastIndex]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_LastItemToDefaultValue(int count)
    {
        if (count > 0 && !IsReadOnly && DefaultValueAllowed)
        {
            var list = GenericIListFactory(count);
            var lastIndex = count - 1;
            if (DefaultValueAllowed)
            {
                list[lastIndex] = default!;
                Assert.Equal(default, list[lastIndex]);
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => list[lastIndex] = default!);
                Assert.NotEqual(default, list[lastIndex]);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_DuplicateValues(int count)
    {
        if (count >= 2 && !IsReadOnly && DuplicateValuesAllowed)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            list[0] = value;
            list[1] = value;
            Assert.Equal(value, list[0]);
            Assert.Equal(value, list[1]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_ItemSet_InvalidValue(int count)
    {
        if (count > 0 && !IsReadOnly)
        {

            foreach (var invalidValue in InvalidValues)
            {
                var list = GenericIListFactory(count);
                Assert.Throws<ArgumentException>(() => list[count / 2] = invalidValue);
            }
        }
    }

    #endregion

    #region IndexOf

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_IndexOf_DefaultValueNotContainedInList(int count)
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
    public void IList_Generic_IndexOf_DefaultValueContainedInList(int count)
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
    public void IList_Generic_IndexOf_ValidValueNotContainedInList(int count)
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
    public void IList_Generic_IndexOf_ValueInCollectionMultipleTimes(int count)
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
    public void IList_Generic_IndexOf_EachValueNoDuplicates(int count)
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
    public void IList_Generic_IndexOf_InvalidValue(int count)
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
    public void IList_Generic_IndexOf_ReturnsFirstMatchingValue(int count)
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

    #region Insert

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            var validAdd = CreateT(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, validAdd));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(int.MinValue, validAdd));
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_IndexGreaterThanListCount_Appends(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            var validAdd = CreateT(12350);
            list.Insert(count, validAdd);
            Assert.Equal(count + 1, list.Count);
            Assert.Equal(validAdd, list[count]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_ToReadOnlyList(int count)
    {
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            Assert.Throws<NotSupportedException>(() => list.Insert(count / 2, CreateT(321432)));
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_FirstItemToNonDefaultValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            list.Insert(0, value);
            Assert.Equal(value, list[0]);
            Assert.Equal(count + 1, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_FirstItemToDefaultValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed)
        {
            var list = GenericIListFactory(count);
            var value = default(T);
            list.Insert(0, value!);
            Assert.Equal(value, list[0]);
            Assert.Equal(count + 1, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_LastItemToNonDefaultValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            var lastIndex = count > 0 ? count - 1 : 0;
            list.Insert(lastIndex, value);
            Assert.Equal(value, list[lastIndex]);
            Assert.Equal(count + 1, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_LastItemToDefaultValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed)
        {
            var list = GenericIListFactory(count);
            var value = default(T);
            var lastIndex = count > 0 ? count - 1 : 0;
            list.Insert(lastIndex, value!);
            Assert.Equal(value, list[lastIndex]);
            Assert.Equal(count + 1, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_DuplicateValues(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
        {
            var list = GenericIListFactory(count);
            var value = CreateT(123452);
            if (AddRemoveClear_ThrowsNotSupported)
            {
                Assert.Throws<NotSupportedException>(() => list.Insert(0, value));
            }
            else
            {
                list.Insert(0, value);
                list.Insert(1, value);
                Assert.Equal(value, list[0]);
                Assert.Equal(value, list[1]);
                Assert.Equal(count + 2, list.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_Insert_InvalidValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            foreach (var value in InvalidValues)
            {
                var list = GenericIListFactory(count);
                Assert.Throws<ArgumentException>(() => list.Insert(count / 2, value));
            }
        }
    }

    #endregion

    #region RemoveAt

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_RemoveAt_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            _ = CreateT(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(int.MinValue));
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_RemoveAt_IndexGreaterThanListCount_ThrowsArgumentOutOfRangeException(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            _ = CreateT(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count + 1));
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_RemoveAt_OnReadOnlyList(int count)
    {
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(count / 2));
            Assert.Equal(count, list.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_RemoveAt_AllValidIndices(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            Assert.Equal(count, list.Count);
            foreach (var i in Enumerable.Range(0, count).Reverse())
            {
                list.RemoveAt(i);
                Assert.Equal(i, list.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_RemoveAt_ZeroMultipleTimes(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var list = GenericIListFactory(count);
            foreach (var i in Enumerable.Range(0, count))
            {
                list.RemoveAt(0);
                Assert.Equal(count - i - 1, list.Count);
            }
        }
    }

    #endregion

    #region Enumerator.Current

    // Test Enumerator.Current at end after new elements was added
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IList_Generic_CurrentAtEnd_AfterAdd(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericIListFactory(count);

            using IEnumerator<T> enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            T? current;
            if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current); // enumerator.Current should fail
            }
            else
            {
                current = enumerator.Current;
                Assert.Equal(default, current);
            }

            // Test after add
            var seed = 3538963;
            for (var i = 0; i < 3; i++)
            {
                collection.Add(CreateT(seed++));

                if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
                {
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current); // enumerator.Current should fail
                }
                else
                {
                    current = enumerator.Current;
                    Assert.Equal(default, current);
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// Helper class to provide means to modify an enumerable, which is not the to be tested type.
/// </summary>
internal class ModifyEnumerableList<T>(Func<int, T> createT) : IListTestSuite<T>
{
    protected override T CreateT(int seed)
    {
        return createT(seed);
    }

    protected override IList<T> GenericIListFactory()
    {
        throw new NotImplementedException();
    }
}