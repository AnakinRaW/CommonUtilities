using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// A read-only variant of the <see cref="FrugalList{T}"/>.
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

    /// <inheritdoc cref="IReadOnlyList{T}"/>
    public int Count => _list.Count;

    /// <inheritdoc cref="IReadOnlyList{T}"/>
    public T this[int index] => _list[index];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableFrugalList{T}"/> structure to one item.
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

    T IList<T>.this[int index]
    {
        get => _list[index];
        set => throw new NotSupportedException("Collection is read-only.");
    }

    bool ICollection<T>.IsReadOnly => true;

    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Collection is read-only.");

    bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");

    void IList<T>.RemoveAt(int index) => throw new NotSupportedException("Collection is read-only.");

    void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only.");

    #endregion

    #region Linq Re-Implemenations

    // Natively implementing frequent Linq functions avoids boxing. Add more if necessary.

    /// <summary>
    /// Creates a <see cref="List{T}"/> from the <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> that contains elements from the <see cref="FrugalList{T}"/>.</returns>
    public List<T> ToList()
    {
        return _list.ToList();
    }

    /// <summary>
    /// Copies the elements of the <see cref="FrugalList{T}"/> to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements of the <see cref="FrugalList{T}"/>.</returns>
    public T[] ToArray()
    {
        return _list.ToArray();
    }

    /// <summary>
    /// Returns the first element of the <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <returns>The first element of the specified <see cref="FrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="FrugalList{T}"/> is empty.</exception>
    public T First()
    {
        return _list.First();
    }

    /// <summary>
    /// Returns the last element of the <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <returns>The last element of the specified <see cref="FrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="FrugalList{T}"/> is empty.</exception>
    public T Last()
    {
        return _list.Last();
    }

    /// <summary>
    /// Returns the first element of the <see cref="FrugalList{T}"/>, or a default value if no element is found.
    /// </summary>
    /// <returns><see langword="default(T)"/> if source is empty; otherwise, the first element in source.</returns>
    public T? FirstOrDefault()
    {
        return _list.FirstOrDefault();
    }

    /// <summary>
    /// Returns the last element of the <see cref="FrugalList{T}"/>, or a default value if no element is found.
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

public static class ImmutableFrugalList
{
    public static ImmutableFrugalList<T> Create<T>(IEnumerable<T> items)
    {
        if (items == null)
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
}