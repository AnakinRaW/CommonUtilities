using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Provides an immutable variant of the <see cref="FrugalList{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
[DebuggerTypeProxy(typeof(IReadOnlyCollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public readonly struct ImmutableFrugalList<T> : IList<T>, IReadOnlyList<T>
{
    /// <summary>
    /// Returns an empty <see cref="ImmutableFrugalList{T}"/> that has the specified type argument.
    /// </summary>
    public static readonly ImmutableFrugalList<T> Empty = default;

    private readonly FrugalList<T> _list;

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count => _list.Count;

    /// <inheritdoc cref="IReadOnlyList{T}.this"/>
    public T this[int index] => _list[index];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableFrugalList{T}"/> structure with the specified item.
    /// </summary>
    /// <param name="item">The item of the list.</param>
    internal ImmutableFrugalList(T item)
    {
        _list = new FrugalList<T>(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableFrugalList{T}"/> structure from a <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <param name="items">The items of this list.</param>
    /// <remarks>
    /// Modifications to <paramref name="items"/> will not be reflected to this instance.
    /// </remarks>
    internal ImmutableFrugalList(in FrugalList<T> items)
    {
        _list = new FrugalList<T>(in items);
    }

    /// <inheritdoc cref="ICollection{T}.CopyTo"/>
    public void CopyTo(T[] array, int index)
    {
        _list.CopyTo(array, index);
    }

    #region Explixit IList/ICollection<T> implementations

    /// <summary>
    /// Gets the element at the specified index. An <see cref="NotSupportedException"/> occurs if you try to set the item at the specified index.
    /// </summary>
    /// <remarks>
    /// Because the collection is immutable, you can only get this item at the specified index.
    /// An exception will occur if you try to set the item. This member is an explicit interface member implementation.
    /// It can be used only when the <see cref="ImmutableFrugalList{T}"/> instance is cast to an <see cref="IList{T}"/> interface.
    /// </remarks>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <value>The element at the specified index.</value>
    /// <exception cref="NotSupportedException">The element at the specified index.</exception>
    T IList<T>.this[int index]
    {
        get => _list[index];
        set => throw new NotSupportedException("Collection is read-only.");
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the <see cref="ICollection{T}"/> is read-only; otherwise, <see langword="false"/>.
    /// In the implementation of <see cref="ImmutableFrugalList{T}"/>, this property always returns <see langword="true"/>.
    /// </value>
    bool ICollection<T>.IsReadOnly => true;

    /// <summary>
    /// Adds an item to the <see cref="ICollection{T}"/>. This implementation always throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Inserts an item to the <see cref="IList{T}"/> at the specified index.
    /// This implementation always throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="IList{T}"/>.</param>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Collection is read-only.");

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.
    /// This implementation always throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="item">The object to remove from the list.</param>
    /// <returns>
    /// Always throws a <see cref="NotSupportedException"/> because the collection is read-only.
    /// </returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");

    /// <summary>
    /// Removes the <see cref="IList{T}"/> item at the specified index. 
    /// This implementation always throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException("Collection is read-only.");

    /// <summary>
    /// Removes all items from the <see cref="ICollection{T}"/>.
    /// This implementation always throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only.");

    #endregion

    #region Linq Re-Implemenations

    // Natively implementing frequent Linq functions avoids boxing. Add more if necessary.

    /// <summary>
    /// Creates a <see cref="List{T}"/> from the <see cref="ImmutableFrugalList{T}"/>.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> that contains elements from the <see cref="ImmutableFrugalList{T}"/>.</returns>
    public List<T> ToList()
    {
        return _list.ToList();
    }

    /// <summary>
    /// Copies the elements of the <see cref="ImmutableFrugalList{T}"/> to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements of the <see cref="ImmutableFrugalList{T}"/>.</returns>
    public T[] ToArray()
    {
        return _list.ToArray();
    }

    /// <summary>
    /// Returns the first element of the <see cref="ImmutableFrugalList{T}"/>.
    /// </summary>
    /// <returns>The first element of the specified <see cref="ImmutableFrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="ImmutableFrugalList{T}"/> is empty.</exception>
    public T First()
    {
        return _list.First();
    }

    /// <summary>
    /// Returns the last element of the <see cref="ImmutableFrugalList{T}"/>.
    /// </summary>
    /// <returns>The last element of the specified <see cref="ImmutableFrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="ImmutableFrugalList{T}"/> is empty.</exception>
    public T Last()
    {
        return _list.Last();
    }

    /// <summary>
    /// Returns the first element of the <see cref="ImmutableFrugalList{T}"/>, or a default value if no element is found.
    /// </summary>
    /// <returns><see langword="default(T)"/> if source is empty; otherwise, the first element in source.</returns>
    public T? FirstOrDefault()
    {
        return _list.FirstOrDefault();
    }

    /// <summary>
    /// Returns the last element of the <see cref="ImmutableFrugalList{T}"/>, or a default value if no element is found.
    /// </summary>
    /// <returns><see langword="default(T)"/> if source is empty; otherwise, the last element in source.</returns>
    public T? LastOrDefault()
    {
        return _list.LastOrDefault();
    }

    /// <summary>
    /// Determines whether the <see cref="ImmutableFrugalList{T}"/> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ImmutableFrugalList{T}"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="ImmutableFrugalList{T}"/>; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    { 
        return _list.Contains(item);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="ImmutableFrugalList{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ImmutableFrugalList{T}"/>. The value can be <see langword="null"/> for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item"/> within the entire <see cref="ImmutableFrugalList{T}"/>, if found; otherwise, -1.</returns>
    public int IndexOf(T item)
    {
        return _list.IndexOf(item);
    }

    #endregion

    /// <summary>
    /// Returns an enumerator that iterates through the immutable list.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the immutable list.</returns>
    public FrugalList<T>.Enumerator GetEnumerator()
    {
        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        return _list.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
    /// </returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        return Count == 0
            ? EmptyEnumerator<T>.Instance
            : _list.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
}

/// <summary>
/// Provides static methods for immutable frugal lists.
/// </summary>
public static class ImmutableFrugalList
{
    /// <summary>
    /// Creates a new instance of <see cref="ImmutableFrugalList{T}"/> from the specified collection of items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="items">The collection of items to initialize the list with.</param>
    /// <returns>
    /// An <see cref="ImmutableFrugalList{T}"/> containing the elements from the specified collection.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    public static ImmutableFrugalList<T> Create<T>(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items is ImmutableFrugalList<T> immutable)
            return immutable;
        if (items is FrugalList<T> frugal)
            return new ImmutableFrugalList<T>(in frugal);
        if (items is ICollection<T> { Count: 0 })
            return ImmutableFrugalList<T>.Empty;
        if (items is IList<T> { Count: 1 } list)
            return new ImmutableFrugalList<T>(list[0]);
        return new FrugalList<T>(items).ToImmutableList();
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="ImmutableFrugalList{T}"/> containing a single specified item.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="item">The single item to include in the list.</param>
    /// <returns>An <see cref="ImmutableFrugalList{T}"/> containing the specified item.</returns>
    public static ImmutableFrugalList<T> Single<T>(T item)
    {
        return new ImmutableFrugalList<T>(item);
    }
}