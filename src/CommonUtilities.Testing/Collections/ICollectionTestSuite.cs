using AnakinRaW.CommonUtilities.Testing.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace AnakinRaW.CommonUtilities.Testing.Collections;

// This test suite is taken from the .NET runtime repository (https://github.com/dotnet/runtime) and adapted to the VSTesting Framework.
// The .NET Foundation licenses this under the MIT license.
/// <summary>
/// Contains tests that ensure the correctness of any class that implements the generic
/// <see cref="ICollection{T}"/> interface
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class ICollectionTestSuite<T> : IEnumerableTestSuite<T>
{
    /// <summary>
    /// Gets the <see cref="Type"/> of the exception that is expected to be thrown
    /// when attempting to copy elements of the collection to an array with a starting index
    /// larger than the array's length.
    /// </summary>
    /// <remarks>
    /// By default, this property returns <see cref="ArgumentException"/>.
    /// Override this property in derived classes to specify a different exception type
    /// if the behavior differs.
    /// </remarks>
    protected virtual Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentException);

    /// <summary>
    /// Gets a collection of values that are considered invalid for the test suite.
    /// </summary>
    /// <remarks>
    /// These values are used in various test cases to validate the behavior of the collection
    /// when handling invalid inputs. The specific definition of "invalid" depends on the context
    /// of the test suite and the type parameter <typeparamref name="T"/>.
    /// </remarks>
    protected virtual IEnumerable<T> InvalidValues => [];

    /// <summary>
    /// Gets a value indicating whether the default value of the type <typeparamref name="T"/> is allowed
    /// to be added to the collection.
    /// </summary>
    /// <remarks>
    /// This property determines whether operations involving the default value of <typeparamref name="T"/>
    /// (e.g., adding or checking for the default value) are valid within the collection.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if the default value of <typeparamref name="T"/> is allowed in the collection; otherwise, <see langword="false"/>.
    /// </value>
    protected virtual bool DefaultValueAllowed => true;

    /// <summary>
    /// Gets a value indicating whether the <c>Add</c>, <c>Remove</c>, and <c>Clear</c> operations 
    /// are expected to throw a <see cref="NotSupportedException"/> in the collection.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the <c>Add</c>, <c>Remove</c>, and <c>Clear</c> operations 
    /// are not supported and should throw a <see cref="NotSupportedException"/>; otherwise, <see langword="false"/>.
    /// </value>
    protected virtual bool AddRemoveClear_ThrowsNotSupported => false;

    /// <summary>
    /// Indicates whether the collection supports storing duplicate values.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the collection allows multiple instances of the same value; otherwise, <see langword="false"/>.
    /// </value>
    protected virtual bool DuplicateValuesAllowed => true;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    /// <remarks>
    /// A read-only collection does not allow the addition, removal, or modification of elements
    /// after the collection is created. This property can be overridden to specify whether the
    /// collection is read-only in derived classes.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if the collection is read-only; otherwise, <see langword="false"/>.
    /// </value>
    protected virtual bool IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether an exception is thrown when attempting to use the default value
    /// in a collection where default values are not allowed.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if an exception is thrown when the default value is used in such a collection; 
    /// otherwise, <see langword="false"/>.
    /// </value>
    protected virtual bool DefaultValueWhenNotAllowed_Throws => true;

    /// <summary>
    /// Creates an instance of an <see cref="ICollection{T}"/> that can be used for testing.
    /// </summary>
    /// <returns>An instance of an <see cref="ICollection{T}"/> that can be used for testing.</returns>
    protected abstract ICollection<T> GenericICollectionFactory();

    /// <summary>
    /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
    /// </summary>
    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Add))
        {
            yield return enumerable =>
            {
                var casted = (ICollection<T>)enumerable;
                casted.Add(CreateT(2344));
                return true;
            };
        }
        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Remove))
        {
            yield return enumerable =>
            {
                var casted = (ICollection<T>)enumerable;
                if (casted.Count > 0)
                {
                    casted.Remove(casted.ElementAt(0));
                    return true;
                }
                return false;
            };
        }
        if (!AddRemoveClear_ThrowsNotSupported && operations.HasFlag(ModifyOperation.Clear))
        {
            yield return enumerable =>
            {
                var casted = (ICollection<T>)enumerable;
                if (casted.Count > 0)
                {
                    casted.Clear();
                    return true;
                }
                return false;
            };
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<T> GenericIEnumerableFactory(int count)
    {
        return GenericICollectionFactory(count);
    }

    /// <summary>
    /// Creates an instance of an <see cref="ICollection{T}"/> that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned <see cref="ICollection{T}"/> contains.</param>
    /// <returns>An instance of an <see cref="ICollection{T}"/> that can be used for testing.</returns>
    protected virtual ICollection<T> GenericICollectionFactory(int count)
    {
        var collection = GenericICollectionFactory();
        AddToCollection(collection, count);
        return collection;
    }

    /// <summary>
    /// Adds a specified number of items to the given collection.
    /// </summary>
    /// <param name="collection">The collection to which items will be added.</param>
    /// <param name="numberOfItemsToAdd">The number of items to add to the collection.</param>
    protected virtual void AddToCollection(ICollection<T> collection, int numberOfItemsToAdd)
    {
        var seed = 9600;
        var comparer = GetIEqualityComparer();
        while (collection.Count < numberOfItemsToAdd)
        {
            var toAdd = CreateT(seed++);
            while (collection.Contains(toAdd, comparer) || InvalidValues.Contains(toAdd, comparer))
                toAdd = CreateT(seed++);
            collection.Add(toAdd);
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #region Count

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Count_Validity(int count)
    {
        var collection = GenericICollectionFactory(count);
        Assert.Equal(count, collection.Count);
    }

    #endregion

    #region Add

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void ICollection_Generic_Add_DefaultValue(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            collection.Add(default!);
            Assert.Equal(count + 1, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_InvalidValueToMiddleOfCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            foreach (var invalidValue in InvalidValues)
            {
                var collection = GenericICollectionFactory(count);
                collection.Add(invalidValue);
                for (var i = 0; i < count; i++)
                    collection.Add(CreateT(i));
                Assert.Equal(count * 2, collection.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_InvalidValueToBeginningOfCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            foreach (var invalidValue in InvalidValues)
            {
                var collection = GenericICollectionFactory(0);
                collection.Add(invalidValue);
                for (var i = 0; i < count; i++)
                    collection.Add(CreateT(i));
                Assert.Equal(count, collection.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_InvalidValueToEndOfCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            foreach (var invalidValue in InvalidValues)
            {
                var collection = GenericICollectionFactory(count);
                collection.Add(invalidValue);
                Assert.Equal(count, collection.Count);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_DuplicateValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
        {
            var collection = GenericICollectionFactory(count);
            var duplicateValue = CreateT(700);
            collection.Add(duplicateValue);
            collection.Add(duplicateValue);
            Assert.Equal(count + 2, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_AfterCallingClear(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            collection.Clear();
            AddToCollection(collection, 5);
            Assert.Equal(5, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_AfterRemovingAnyValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var seed = 840;
            var collection = GenericICollectionFactory(count);
            var items = collection.ToList();
            var toAdd = CreateT(seed++);
            while (collection.Contains(toAdd))
                toAdd = CreateT(seed++);
            collection.Add(toAdd);
            collection.Remove(toAdd);

            toAdd = CreateT(seed++);
            while (collection.Contains(toAdd))
                toAdd = CreateT(seed++);

            collection.Add(toAdd);
            items.Add(toAdd);
            Assert.EqualUnordered(items, collection);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_AfterRemovingAllItems(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            for (var i = 0; i < count; i++)
                collection.Remove(collection.ElementAt(0));
            collection.Add(CreateT(254));
            Assert.Equal(1, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_ToReadOnlyCollection(int count)
    {
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            Assert.Throws<NotSupportedException>(() => collection.Add(CreateT(0)));
            Assert.Equal(count, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Add_AfterRemoving(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var seed = 840;
            var collection = GenericICollectionFactory(count);
            var toAdd = CreateT(seed++);
            while (collection.Contains(toAdd))
                toAdd = CreateT(seed++);
            collection.Add(toAdd);
            collection.Remove(toAdd);
            collection.Add(toAdd);
        }
    }

    #endregion

    #region Clear

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Clear(int count)
    {
        var collection = GenericICollectionFactory(count);
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            Assert.Throws<NotSupportedException>(collection.Clear);
            Assert.Equal(count, collection.Count);
        }
        else
        {
            collection.Clear();
            Assert.Equal(0, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Clear_Repeatedly(int count)
    {
        var collection = GenericICollectionFactory(count);
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            Assert.Throws<NotSupportedException>(collection.Clear);
            Assert.Throws<NotSupportedException>(collection.Clear);
            Assert.Throws<NotSupportedException>(collection.Clear);
            Assert.Equal(count, collection.Count);
        }
        else
        {
            collection.Clear();
            collection.Clear();
            collection.Clear();
            Assert.Equal(0, collection.Count);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ICollection_Generic_Remove_ReferenceRemovedFromCollection(bool useRemove)
    {
        var isOnMono = Type.GetType("Mono.RuntimeStructs") != null;
        if (isOnMono || typeof(T).IsValueType || IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            return;

        var collection = GenericICollectionFactory();

        var wr = PopulateAndRemove(collection, useRemove);
        Assert.True(SpinWait.SpinUntil(() =>
        {
            GC.Collect();
            return !wr.TryGetTarget(out _);
        }, 30_000));
        GC.KeepAlive(collection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference<object> PopulateAndRemove(ICollection<T> collection, bool useRemove)
        {
            AddToCollection(collection, 1);
            var value = collection.First();

            if (useRemove)
                Assert.True(collection.Remove(value));
            else
            {
                collection.Clear();
                Assert.Equal(0, collection.Count);
            }

            return new WeakReference<object>(value!);
        }
    }

    #endregion

    #region Contains

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Contains_ValidValueOnCollectionNotContainingThatValue(int count)
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
    public void ICollection_Generic_Contains_ValidValueOnCollectionContainingThatValue(int count)
    {
        var collection = GenericICollectionFactory(count);
        foreach (var item in collection)
            Assert.True(collection.Contains(item));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Contains_DefaultValueOnCollectionNotContainingDefaultValue(int count)
    {
        var collection = GenericICollectionFactory(count);
        if (DefaultValueAllowed && default(T) is null) // it's true only for reference types and for Nullable<T>
        {
            Assert.False(collection.Contains(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void ICollection_Generic_Contains_DefaultValueOnCollectionContainingDefaultValue(int count)
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
    public void ICollection_Generic_Contains_ValidValueThatExistsTwiceInTheCollection(int count)
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
    public void ICollection_Generic_Contains_InvalidValue_ThrowsArgumentException(int count)
    {
        var collection = GenericICollectionFactory(count);
        foreach (var value in InvalidValues)
        {
            Assert.Throws<ArgumentException>(() => collection.Contains(value));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void ICollection_Generic_Contains_DefaultValueWhenNotAllowed(int count)
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

    #region CopyTo

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_NullArray_ThrowsArgumentNullException(int count)
    {
        var collection = GenericICollectionFactory(count);
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
    {
        var collection = GenericICollectionFactory(count);
        var array = new T[count];
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, int.MinValue));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
    {
        var collection = GenericICollectionFactory(count);
        var array = new T[count];
        if (count > 0)
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, count));
        else
            collection.CopyTo(array, count); // does nothing since the array is empty
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
    {
        var collection = GenericICollectionFactory(count);
        var array = new T[count];
        Assert.Throws(ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType, () => collection.CopyTo(array, count + 1));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
    {
        if (count > 0) // Want the T array to have at least 1 element
        {
            var collection = GenericICollectionFactory(count);
            var array = new T[count];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 1));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_ExactlyEnoughSpaceInArray(int count)
    {
        var collection = GenericICollectionFactory(count);
        var array = new T[count];
        collection.CopyTo(array, 0);
        Assert.True(collection.SequenceEqual(array));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_CopyTo_ArrayIsLargerThanCollection(int count)
    {
        var collection = GenericICollectionFactory(count);
        var array = new T[count * 3 / 2];
        collection.CopyTo(array, 0);
        Assert.True(collection.SequenceEqual(array.Take(count)));
    }

    #endregion

    #region Remove

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_OnReadOnlyFrugalList_ThrowsNotSupportedException(int count)
    {
        if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            Assert.Throws<NotSupportedException>(() => collection.Remove(CreateT(34543)));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_DefaultValueNotContainedInCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed && !InvalidValues.Contains(default))
        {
            var collection = GenericICollectionFactory(count);
            var value = default(T);
            while (collection.Contains(value!))
            {
                collection.Remove(value!);
                count--;
            }
            Assert.False(collection.Remove(value!));
            Assert.Equal(count, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_NonDefaultValueNotContainedInCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var seed = count * 251;
            var collection = GenericICollectionFactory(count);
            var value = CreateT(seed++);
            while (collection.Contains(value) || InvalidValues.Contains(value))
                value = CreateT(seed++);
            Assert.False(collection.Remove(value));
            Assert.Equal(count, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void ICollection_Generic_Remove_DefaultValueContainedInCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed && !InvalidValues.Contains(default))
        {
            var collection = GenericICollectionFactory(count);
            var value = default(T);
            if (!collection.Contains(value!))
            {
                collection.Add(value!);
                count++;
            }
            Assert.True(collection.Remove(value!));
            Assert.Equal(count - 1, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_NonDefaultValueContainedInCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var seed = count * 251;
            var collection = GenericICollectionFactory(count);
            var value = CreateT(++seed);
            if (!collection.Contains(value))
            {
                collection.Add(value);
                count++;
            }
            Assert.True(collection.Remove(value));
            Assert.Equal(count - 1, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_ValueThatExistsTwiceInCollection(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
        {
            var seed = count * 90;
            var collection = GenericICollectionFactory(count);
            var value = CreateT(++seed);
            collection.Add(value);
            collection.Add(value);
            count += 2;
            Assert.True(collection.Remove(value));
            Assert.True(collection.Contains(value));
            Assert.Equal(count - 1, collection.Count);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_EveryValue(int count)
    {
        if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            foreach (var value in collection.ToList()) 
                Assert.True(collection.Remove(value));
            Assert.False(collection.Any());
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_InvalidValue_ThrowsArgumentException(int count)
    {
        var collection = GenericICollectionFactory(count);
        foreach (var value in InvalidValues) 
            Assert.Throws<ArgumentException>(() => collection.Remove(value));
        Assert.Equal(count, collection.Count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Remove_DefaultValueWhenNotAllowed(int count)
    {
        if (!DefaultValueAllowed && !IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
        {
            var collection = GenericICollectionFactory(count);
            if (DefaultValueWhenNotAllowed_Throws)
                Assert.Throws<ArgumentNullException>(() => collection.Remove(default!));
            else
                Assert.False(collection.Remove(default!));
        }
    }

    #endregion

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}