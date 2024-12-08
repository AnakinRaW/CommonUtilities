using System;
using System.Collections;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// A read-only variant of the <see cref="FrugalList{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public readonly struct ReadOnlyFrugalList<T> : IReadOnlyList<T>
{
    /// <summary>
    /// Returns an empty <see cref="ReadOnlyFrugalList{T}"/> that has the specified type argument.
    /// </summary>
    public static readonly ReadOnlyFrugalList<T> Empty = default;

    private readonly FrugalList<T> _list;

    /// <inheritdoc />
    public int Count => _list.Count;

    /// <inheritdoc />
    public T this[int index] => _list[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyFrugalList{T}"/> structure to one item.
    /// </summary>
    /// <param name="item">The item of the list.</param>
    public ReadOnlyFrugalList(T item)
    {
        _list = new FrugalList<T>(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyFrugalList{T}"/> structure with the given enumerable.
    /// </summary>
    /// <param name="items">The items of this list.</param>
    public ReadOnlyFrugalList(IEnumerable<T> items)
    {
        _list = new FrugalList<T>(items);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyFrugalList{T}"/> structure from a <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <param name="items">The items of this list.</param>
    /// <remarks>
    /// Modifications to <paramref name="items"/> will not be reflected to this instance.
    /// </remarks>
    internal ReadOnlyFrugalList(in FrugalList<T> items)
    {
        _list = new FrugalList<T>(in items);
    }


    /// <inheritdoc cref="ICollection{T}.CopyTo"/>
    public void CopyTo(T[] array, int index)
    {
        _list.CopyTo(array, index);
    }

    #region Linq Re-Implemenations

    // Natively implementing frequent Linq functions avoids boxing. Add more if necessary.

    /// <summary>
    /// Creates a <see cref="List{T}"/> from an this instance.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> that contains elements from this list.</returns>
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
    /// Determines whether the <see cref="ReadOnlyFrugalList{T}"/> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ReadOnlyFrugalList{T}"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="ReadOnlyFrugalList{T}"/>; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item)
    { 
        return _list.Contains(item);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="ReadOnlyFrugalList{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ReadOnlyFrugalList{T}"/>. The value can be <see langword="null"/> for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item"/> within the entire <see cref="ReadOnlyFrugalList{T}"/>, if found; otherwise, -1.</returns>
    public int IndexOf(T item)
    {
        return _list.IndexOf(item);
    }

    #endregion

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ReadOnlyFrugalList{T}"/>
    /// </summary>
    /// <returns>A <see cref="FrugalList{T}.FrugalEnumerator"/> for the <see cref="ReadOnlyFrugalList{T}"/>.</returns>
    public FrugalList<T>.FrugalEnumerator GetEnumerator() => _list.GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}