using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace AnakinRaW.CommonUtilities.Testing.Collections;

// This test suite is taken from the .NET runtime repository (https://github.com/dotnet/runtime) and adapted to the VSTesting Framework.
// The .NET Foundation licenses this under the MIT license.
/// <summary>
/// Contains tests that ensure the correctness of any class that implements the generic
/// IEnumerable interface.
/// </summary>
[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class IEnumerableTestSuite<T> : CollectionsTestSuite<T>
{
    /// <summary>
    /// An enum to allow specification of the order of the Enumerable. Used in validation for enumerables.
    /// </summary>
    protected enum EnumerableOrder
    {
        /// <summary>
        /// Specifies that the enumerable returns in an unspecified order.
        /// </summary>
        Unspecified,
        /// <summary>
        /// Specifies that the enumerable returns sequential.
        /// </summary>
        Sequential
    }

    /// <summary>
    /// Modifies the given IEnumerable such that any enumerators for that IEnumerable will be
    /// invalidated.
    /// </summary>
    /// <param name="enumerable">An IEnumerable to modify</param>
    /// <returns>true if the enumerable was successfully modified. Else false.</returns>
    public delegate bool ModifyEnumerable(IEnumerable<T> enumerable);

    /// <summary>
    /// The Reset method is provided for COM interoperability. It does not necessarily need to be
    /// implemented; instead, the implementer can simply throw a NotSupportedException.
    ///
    /// If Reset is not implemented, this property must return False. The default value is true.
    /// </summary>
    protected virtual bool ResetImplemented => true;

    /// <summary>Whether the enumerator returned from GetEnumerator is a singleton instance when the collection is empty.</summary>
    protected virtual bool Enumerator_Empty_UsesSingletonInstance => false;

    /// <summary>
    /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
    /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
    /// to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Returning an undefined value.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// false.
    /// </summary>
    protected virtual bool Enumerator_Current_UndefinedOperation_Throws => false;

    /// <summary>
    /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
    /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
    /// to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Returning an undefined value.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// false.
    /// </summary>
    protected virtual bool NonGenericEnumerator_Current_UndefinedOperation_Throws => false;

    /// <summary>
    /// When calling Current of the empty enumerator before the first MoveNext, after the end of the collection,
    /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
    /// to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Returning an undefined value.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// <see cref="Enumerator_Current_UndefinedOperation_Throws"/>.
    /// </summary>
    protected virtual bool Enumerator_Empty_Current_UndefinedOperation_Throws => Enumerator_Current_UndefinedOperation_Throws;

    /// <summary>
    /// When calling Current of the empty enumerator before the first MoveNext, after the end of the collection,
    /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
    /// to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Returning an undefined value.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// false.
    /// </summary>
    protected virtual bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => Enumerator_Current_UndefinedOperation_Throws;

    /// <summary>
    /// Specifies whether this IEnumerable follows some sort of ordering pattern.
    /// </summary>
    protected virtual EnumerableOrder Order => EnumerableOrder.Sequential;

    /// <summary>
    /// When calling MoveNext or Reset after modification of the enumeration, the resulting behavior is
    /// undefined. Tests are included to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Execute MoveNext or Reset.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// true.
    /// </summary>
    protected virtual bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => true;

    /// <summary>
    /// When calling MoveNext or Reset after modification of an empty enumeration, the resulting behavior is
    /// undefined. Tests are included to cover two behavioral scenarios:
    ///   - Throwing an InvalidOperationException
    ///   - Execute MoveNext or Reset.
    ///
    /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
    /// <see cref="Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException"/>.
    /// </summary>
    protected virtual bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException;

    /// <summary>
    /// Gets the set of <see cref="CollectionsTestSuite.ModifyOperation"/> values that represent modifications to a collection
    /// which are expected to throw an <see cref="InvalidOperationException"/> when performed during enumeration.
    /// </summary>
    /// <remarks>
    /// This property defines the operations that are not allowed to be performed on a collection
    /// while it is being enumerated. By default, it includes <see cref="CollectionsTestSuite.ModifyOperation.Add"/>, 
    /// <see cref="CollectionsTestSuite.ModifyOperation.Insert"/>, <see cref="CollectionsTestSuite.ModifyOperation.Overwrite"/>, 
    /// <see cref="CollectionsTestSuite.ModifyOperation.Remove"/>, and <see cref="CollectionsTestSuite.ModifyOperation.Clear"/>.
    /// </remarks>
    protected virtual ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

    /// <summary>
    /// Gets a value indicating the types of modification operations that are allowed on an enumerator
    /// during enumeration without causing exceptions.
    /// </summary>
    /// <remarks>
    /// This property specifies the set of <see cref="CollectionsTestSuite.ModifyOperation"/> flags that represent
    /// the operations permitted on the enumerator while it is being enumerated. By default, no
    /// modifications are allowed, as indicated by <see cref="CollectionsTestSuite.ModifyOperation.None"/>.
    /// </remarks>
    protected virtual ModifyOperation ModifyEnumeratorAllowed => ModifyOperation.None;

    /// <summary>
    /// Creates an instance of an IEnumerable{T} that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned IEnumerable{T} contains.</param>
    /// <returns>An instance of an IEnumerable{T} that can be used for testing.</returns>
    protected abstract IEnumerable<T> GenericIEnumerableFactory(int count);

    /// <summary>
    /// To be implemented in the concrete collections test classes. Returns a set of ModifyEnumerable delegates
    /// that modify the enumerable passed to them.
    /// </summary>
    protected abstract IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations);

    /// <summary>
    /// Creates an instance of an IEnumerable that can be used for testing.
    /// </summary>
    /// <param name="count">The number of unique items that the returned IEnumerable contains.</param>
    /// <returns>An instance of an IEnumerable that can be used for testing.</returns>
    protected virtual IEnumerable NonGenericIEnumerableFactory(int count)
    {
        return GenericIEnumerableFactory(count);
    }

    private void RepeatTest(Action<IEnumerator<T>, T[]> testCode, int iters = 3)
    {
        RepeatTest((e, i, _) => testCode(e, i), iters);
    }

    private void RepeatTest(Action<IEnumerator<T>, T[], int> testCode, int iters = 3)
    {
        var enumerable = GenericIEnumerableFactory(32);
        var items = enumerable.ToArray();
        var enumerator = enumerable.GetEnumerator();
        for (var i = 0; i < iters; i++)
        {
            testCode(enumerator, items, i);
            if (!ResetImplemented)
                enumerator = enumerable.GetEnumerator();
            else
                enumerator.Reset();
        }
        enumerator.Dispose();
    }

    private void VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems)
    {
        VerifyEnumerator(enumerator, expectedItems, 0, expectedItems.Length, true, true);
    }

    private void VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems, int startIndex, int count, bool validateStart, bool validateEnd)
    {
        var needToMatchAllExpectedItems = count - startIndex == expectedItems.Length;
        if (validateStart)
        {
            for (var i = 0; i < 3; i++)
            {
                if (Enumerator_Current_UndefinedOperation_Throws)
                {
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                }
                else
                {
                    _ = enumerator.Current;
                }
            }
        }

        int iterations;
        if (Order == EnumerableOrder.Unspecified)
        {
            var itemsVisited = new BitArray(needToMatchAllExpectedItems ? count : expectedItems.Length, false);
            for (iterations = 0; iterations < count && enumerator.MoveNext(); iterations++)
            {
                object? currentItem = enumerator.Current;

                var itemFound = false;
                for (var i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && Equals(currentItem, expectedItems[i + (needToMatchAllExpectedItems ? startIndex : 0)]))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }

                Assert.True(itemFound, "itemFound");

                for (var i = 0; i < 3; i++)
                {
                    object? tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }
            }

            if (needToMatchAllExpectedItems)
            {
                for (var i = 0; i < itemsVisited.Length; i++)
                {
                    Assert.True(itemsVisited[i]);
                }
            }
            else
            {
                var visitedItemCount = 0;
                for (var i = 0; i < itemsVisited.Length; i++)
                {
                    if (itemsVisited[i])
                    {
                        ++visitedItemCount;
                    }
                }

                Assert.Equal(count, visitedItemCount);
            }
        }
        else if (Order == EnumerableOrder.Sequential)
        {
            for (iterations = 0; iterations < count && enumerator.MoveNext(); iterations++)
            {
                object? currentItem = enumerator.Current;
                Assert.Equal(expectedItems[iterations], currentItem);
                for (var i = 0; i < 3; i++)
                {
                    object? tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }
            }
        }
        else
        {
            throw new ArgumentException(
                "EnumerableOrder is invalid.");
        }

        Assert.Equal(count, iterations);

        if (validateEnd)
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext(), "enumerator.MoveNext() returned true past the expected end.");

                if (Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    _ = enumerator.Current;
            }
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #region GetEnumerator()

    [Fact]
    public void IEnumerable_NonGeneric_GetEnumerator_EmptyCollection_UsesSingleton()
    {
        var enumerable = NonGenericIEnumerableFactory(0);

        var enumerator1 = enumerable.GetEnumerator();
        try
        {
            var enumerator2 = enumerable.GetEnumerator();
            try
            {
                Assert.Equal(Enumerator_Empty_UsesSingletonInstance, ReferenceEquals(enumerator1, enumerator2));
            }
            finally
            {
                if (enumerator2 is IDisposable d2) d2.Dispose();
            }
        }
        finally
        {
            if (enumerator1 is IDisposable d1) d1.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_GetEnumerator_NoExceptionsWhileGetting(int count)
    {
        var enumerable = NonGenericIEnumerableFactory(count);
        // ReSharper disable once GenericEnumeratorNotDisposed
        Assert.NotNull(enumerable.GetEnumerator());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_GetEnumerator_ReturnsUniqueEnumerator(int count)
    {
        //Tests that the enumerators returned by GetEnumerator operate independently of one another
        var enumerable = NonGenericIEnumerableFactory(count);
        var iterations = 0;
        foreach (var _ in enumerable)
        foreach (var __ in enumerable)
        foreach (var ___ in enumerable)
            iterations++;
        Assert.Equal(count * count * count, iterations);
    }

    [Fact]
    public void IEnumerable_Generic_GetEnumerator_EmptyCollection_UsesSingleton()
    {
        IEnumerable enumerable = GenericIEnumerableFactory(0);

        var enumerator1 = enumerable.GetEnumerator();
        try
        {
            var enumerator2 = enumerable.GetEnumerator();
            try
            {
                Assert.Equal(Enumerator_Empty_UsesSingletonInstance, ReferenceEquals(enumerator1, enumerator2));
            }
            finally
            {
                if (enumerator2 is IDisposable d2) d2.Dispose();
            }
        }
        finally
        {
            if (enumerator1 is IDisposable d1) d1.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_GetEnumerator_NoExceptionsWhileGetting(int count)
    {
        var enumerable = GenericIEnumerableFactory(count);
        enumerable.GetEnumerator().Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_GetEnumerator_ReturnsUniqueEnumerator(int count)
    {
        //Tests that the enumerators returned by GetEnumerator operate independently of one another
        var enumerable = GenericIEnumerableFactory(count);
        var iterations = 0;
        foreach (var _ in enumerable)
            foreach (var __ in enumerable)
                foreach (var ___ in enumerable)
                    iterations++;
        Assert.Equal(count * count * count, iterations);
    }

    #endregion

    #region Enumerator.MoveNext

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_MoveNext_FromStartToFinish(int count)
    {
        var iterations = 0;
        var enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
        while (enumerator.MoveNext())
            iterations++;
        Assert.Equal(count, iterations);
        if (enumerator is IDisposable d)
            d.Dispose();
    }


    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_MoveNext_AfterEndOfCollection(int count)
    {
        var enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
        for (var i = 0; i < count; i++)
            enumerator.MoveNext();
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();
            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                else
                    _ = enumerator.MoveNext();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();

            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();

            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                else
                    enumerator.MoveNext();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                else
                    _ = enumerator.MoveNext();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_FromStartToFinish(int count)
    {
        var iterations = 0;
        using var enumerator = GenericIEnumerableFactory(count).GetEnumerator();
        while (enumerator.MoveNext())
            iterations++;
        Assert.Equal(count, iterations);
    }

    /// <summary>
    /// For most collections, all calls to MoveNext after disposal of an enumerator will return false.
    /// Some collections (SortedList), however, treat a call to dispose as if it were a call to Reset. Since the docs
    /// specify neither of these as being strictly correct, we leave the method virtual.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Enumerator_MoveNext_AfterDisposal(int count)
    {
        var enumerator = GenericIEnumerableFactory(count).GetEnumerator();
        for (var i = 0; i < count; i++)
            enumerator.MoveNext();
        enumerator.Dispose();
        Assert.False(enumerator.MoveNext());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_AfterEndOfCollection(int count)
    {
        using var enumerator = GenericIEnumerableFactory(count).GetEnumerator();
        for (var i = 0; i < count; i++)
            enumerator.MoveNext();
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void IEnumerable_Generic_Enumerator_MoveNextHitsAllItems()
    {
        RepeatTest((enumerator, items) =>
        {
            var iterations = 0;
            while (enumerator.MoveNext())
            {
                iterations++;
            }
            Assert.Equal(items.Length, iterations);
        });
    }

    [Fact]
    public void IEnumerable_Generic_Enumerator_MoveNextFalseAfterEndOfCollection()
    {
        RepeatTest((enumerator, _) =>
        {
            while (enumerator.MoveNext())
            {
            }

            Assert.False(enumerator.MoveNext());
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
                else
                {
                    enumerator.MoveNext();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedBeforeEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                if (Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    enumerator.MoveNext();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();
            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
                else
                {
                    enumerator.MoveNext();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedDuringEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();
            if (modifyEnumerable(enumerable))
            {
                enumerator.MoveNext();
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                }
                else
                {
                    enumerator.MoveNext();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_MoveNext_ModifiedAfterEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            if (modifyEnumerable(enumerable))
            {
                enumerator.MoveNext();
            }
        }
    }

    #endregion

    #region Enumerator.Current

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Current_FromStartToFinish(int count)
    {
        var enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
        while (enumerator.MoveNext())
            _ = enumerator.Current;
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
    {
        var enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            Assert.Equal(current, enumerator.Current);
            Assert.Equal(current, enumerator.Current);
            Assert.Equal(current, enumerator.Current);
        }
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Current_ReturnsSameObjectsOnDifferentEnumerators(int count)
    {
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        // Ensures that the elements returned from enumeration are exactly the same collection of
        // elements returned from a previous enumeration
        var enumerable = NonGenericIEnumerableFactory(count);
        var comparer = GetIEqualityComparer();
        var firstValues = new Dictionary<T, int>(count, comparer);
        var secondValues = new Dictionary<T, int>(count, comparer);
        foreach (T item in enumerable)
            firstValues[item] = firstValues.ContainsKey(item) ? firstValues[item]++ : 1;
        foreach (T item in enumerable)
            secondValues[item] = secondValues.ContainsKey(item) ? secondValues[item]++ : 1;
        Assert.Equal(firstValues.Count, secondValues.Count);
        foreach (var key in firstValues.Keys)
            Assert.Equal(firstValues[key], secondValues[key]);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Enumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
    {
        var enumerable = NonGenericIEnumerableFactory(count);
        var enumerator = enumerable.GetEnumerator();
        if (count == 0 ? NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw : NonGenericEnumerator_Current_UndefinedOperation_Throws)
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        else
            _ = enumerator.Current;
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Enumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
    {
        var enumerable = NonGenericIEnumerableFactory(count);
        var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext()) ;
        if (count == 0 ? NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw : NonGenericEnumerator_Current_UndefinedOperation_Throws)
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        else
            _ = enumerator.Current;
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public virtual void Enumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();
            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0 ? NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw : NonGenericEnumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    _ = enumerator.Current;
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Fact]
    public void IEnumerable_Generic_Enumerator_Current()
    {
        // Verify that current returns proper result.
        RepeatTest((enumerator, items, iteration) =>
        {
            if (iteration == 1)
                VerifyEnumerator(enumerator, items, 0, items.Length / 2, true, false);
            else
                VerifyEnumerator(enumerator, items);
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
    {
        using var enumerator = GenericIEnumerableFactory(count).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            Assert.Equal(current, enumerator.Current);
            Assert.Equal(current, enumerator.Current);
            Assert.Equal(current, enumerator.Current);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_ReturnsSameObjectsOnDifferentEnumerators(int count)
    {
        // Ensures that the elements returned from enumeration are exactly the same collection of
        // elements returned from a previous enumeration
        var enumerable = GenericIEnumerableFactory(count);
        var comparer = GetIEqualityComparer();
#pragma warning disable CS8714
        var firstValues = new Dictionary<T, int>(count, comparer);
        var secondValues = new Dictionary<T, int>(count, comparer);
#pragma warning restore CS8714
        foreach (var item in enumerable)
            firstValues[item] = firstValues.ContainsKey(item) ? firstValues[item]++ : 1;
        foreach (var item in enumerable)
            secondValues[item] = secondValues.ContainsKey(item) ? secondValues[item]++ : 1;
        Assert.Equal(firstValues.Count, secondValues.Count);
        foreach (var key in firstValues.Keys)
            Assert.Equal(firstValues[key], secondValues[key]);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
    {
        var enumerable = GenericIEnumerableFactory(count);
        using var enumerator = enumerable.GetEnumerator();
        if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        else
            _ = enumerator.Current;
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
    {
        var enumerable = GenericIEnumerableFactory(count);
        using var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext())
        {
        }

        if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        else
            _ = enumerator.Current;
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                else
                    _ = enumerator.Current;
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Current_ModifiedDuringEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                _ = enumerator.Current;
            }
        }
    }

    #endregion

    #region Enumerator.Reset

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Reset_BeforeIteration_Support(int count)
    {
        var enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
        if (ResetImplemented)
            enumerator.Reset();
        else
            Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        if (enumerator is IDisposable d)
            d.Dispose();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();
            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                else
                    enumerator.Reset();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();

            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();

            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                else
                    enumerator.Reset();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
    {
        Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
        {
            var enumerable = NonGenericIEnumerableFactory(count);
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
            if (ModifyEnumerable((IEnumerable<T>)enumerable))
            {
                if (count == 0
                        ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException
                        : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                else
                    enumerator.Reset();
            }
            if (enumerator is IDisposable d)
                d.Dispose();
        });
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_BeforeIteration_Support(int count)
    {
        using var enumerator = GenericIEnumerableFactory(count).GetEnumerator();
        if (ResetImplemented)
            enumerator.Reset();
        else
            Assert.Throws<NotSupportedException>(enumerator.Reset);
    }

    [Fact]
    public void IEnumerable_Generic_Enumerator_Reset()
    {
        if (!ResetImplemented)
        {
            RepeatTest((enumerator, _) =>
            {
                Assert.Throws<NotSupportedException>(enumerator.Reset);
            });
            RepeatTest((enumerator, items, iter) =>
            {
                if (iter == 1)
                {
                    VerifyEnumerator(enumerator, items, 0, items.Length / 2, true, false);
                    for (var i = 0; i < 3; i++)
                    {
                        Assert.Throws<NotSupportedException>(enumerator.Reset);
                    }

                    VerifyEnumerator(enumerator, items, items.Length / 2, items.Length - items.Length / 2, false, true);
                }
                else if (iter == 2)
                {
                    VerifyEnumerator(enumerator, items);
                    for (var i = 0; i < 3; i++)
                    {
                        Assert.Throws<NotSupportedException>(enumerator.Reset);
                    }

                    VerifyEnumerator(enumerator, items, 0, 0, false, true);
                }
                else
                {
                    VerifyEnumerator(enumerator, items);
                }
            });
        }
        else
        {
            RepeatTest((enumerator, items, iter) =>
            {
                if (iter == 1)
                {
                    VerifyEnumerator(enumerator, items, 0, items.Length / 2, true, false);
                    enumerator.Reset();
                    enumerator.Reset();
                }
                else if (iter == 3)
                {
                    VerifyEnumerator(enumerator, items);
                    enumerator.Reset();
                    enumerator.Reset();
                }
                else
                {
                    VerifyEnumerator(enumerator, items);
                }
            }, 5);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(enumerator.Reset);
                }
                else
                {
                    enumerator.Reset();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedBeforeEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            if (modifyEnumerable(enumerable))
            {
                enumerator.Reset();
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();
            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(enumerator.Reset);
                }
                else
                {
                    enumerator.Reset();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedDuringEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < count / 2; i++)
                enumerator.MoveNext();
            if (modifyEnumerable(enumerable))
            {
                enumerator.Reset();
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorThrows))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            if (modifyEnumerable(enumerable))
            {
                if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException)
                {
                    Assert.Throws<InvalidOperationException>(enumerator.Reset);
                }
                else
                {
                    enumerator.Reset();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IEnumerable_Generic_Enumerator_Reset_ModifiedAfterEnumeration_Succeeds(int count)
    {
        foreach (var modifyEnumerable in GetModifyEnumerables(ModifyEnumeratorAllowed))
        {
            var enumerable = GenericIEnumerableFactory(count);
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
            }

            if (modifyEnumerable(enumerable))
                enumerator.Reset();
        }
    }

    #endregion

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}