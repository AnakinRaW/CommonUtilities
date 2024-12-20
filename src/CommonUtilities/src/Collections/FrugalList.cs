﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AnakinRaW.CommonUtilities.Collections;

// Design Notes for FrugalList<T>:
// 1. This struct is mutable. Thus, it has dangerous side effects such as:
//      var a := FrugalList<int> { 0, 0 }
//      var b := a          // Copy by value!
//      a[0] = 1            // Modification of first item does *not* get reflected to copies.
//      a[1] = 1            // Modification to all remaining items gets reflected to copies.
//      print(b[0])         // prints 0
//      print(b[1])         // prints 1
//
//
// 2. Also, this list does not implement the non-generic IList interface for the following reasons: 
// 2.1. The IList.CopyTo(Array, int) adds complexity due to differences between .NET & C# type systems.
//      E.g.
//      In CLR: int[] is compatible to uint[] as vice versa (see. ECMA335 I.8.7.1 array-element-compatible-with)
//      C#: Forbids this without tricking the compiler by casting to object[].
// 
//      (ReadOnlyCollection<T> has implemented this as a reference.)
//
// 2.2  I rarely ever see the non-generic interfaces used.
//
// 2.3  Ideally this struct would not have any interfaces since using this as an interface causes boxing,
//      which totally makes the idea of this FrugalList obsolete.
// 
// 3. Ultimately this list *could* become a ref-struct. However, this introduces a lot more restrictions which I'm currently not confident they are worth taking.


/// <summary>
/// A memory-optimized strongly typed list which avoids unnecessary memory allocations if none or one item is present.
/// </summary>
/// <remarks>
/// <b>Note</b> that this List is a <see langword="struct"/> and thus is copied by-value and not by-reference.
/// Modifications to an instance only gets reflected partially to all other copies.
/// Precisely, modifications involving the first item may not be reflected.
/// Reason is that the first item is stored directly in an instance-field while the remaining items are stored in a backing-list.
/// <br/>
/// <br/>
/// <para>
/// <b>Usage advise:</b>
/// <para>
/// a) To ensure that all changes get reflected to other variables (including the first item)
/// either box this structure (e.g, to <see cref="IList{T}"/> [this allocates memory though]) or pass this structure as by-<see langword="ref"/>.
/// </para>
/// <para>
/// b) If a copy shall not reflect any changes from its source use <see cref="FrugalList{T}(in FrugalList{T})"/>
/// which creates a full shallow-copy of all items in this list.
/// </para>
/// </para>
/// </remarks>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public struct FrugalList<T> : IList<T>
{
    private static readonly EqualityComparer<T> ItemComparer = EqualityComparer<T>.Default;
    private static readonly EmptyList EmptyDummyList = EmptyList.Instance;

    private T _firstItem = default!;
    private List<T>? _tailList;

    /// <inheritdoc />
    public readonly int Count => _tailList is null ? 0 : 1 + _tailList.Count;

    /// <inheritdoc />
    public readonly bool IsReadOnly => false;

    /// <inheritdoc />
    public T this[int index]
    {
        readonly get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index should be non-negative and less than Count.");
            return index == 0 ? _firstItem : _tailList![index - 1];
        }
        set
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index should be non-negative and less than Count.");
            if (index == 0)
                _firstItem = value;
            else
                _tailList![index - 1] = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrugalList{T}"/> structure to one item.
    /// </summary>
    /// <param name="item">The first item for the new list.</param>
    public FrugalList(T item)
    {
        _firstItem = item;
        _tailList = EmptyDummyList;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrugalList{T}"/> structure that contains elements copied from the specified list.
    /// </summary>
    /// <param name="collection">The list whose elements are copied to the new list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public FrugalList(IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        foreach (var item in collection)
            Add(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrugalList{T}"/> structure that copies all elements from the given list.
    /// </summary>
    /// <param name="list">The list whose elements are copied to the new list.</param>
    /// <remarks>
    /// Modifications to <paramref name="list"/> will not be reflected to this instance.
    /// </remarks>
    public FrugalList(in FrugalList<T> list)
    {
        _firstItem = list._firstItem;
        if (list._tailList is null)
            return;
        _tailList = list._tailList is EmptyList ? EmptyDummyList : new List<T>(list._tailList);
    }

    /// <summary>
    /// Produces a read-only representation of the current state from this list.
    /// </summary>
    /// <remarks>
    /// Modifications to this instance will not be reflected to the newly create readonly list.
    /// </remarks>
    /// <returns>The read-only list.</returns>
    public readonly ReadOnlyFrugalList<T> AsReadOnly()
    {
        return new ReadOnlyFrugalList<T>(in this);
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        if (_tailList == null)
        {
            _firstItem = item;
            _tailList = EmptyDummyList;
        }
        else
        {
            if (_tailList is EmptyList)
                _tailList = new List<T>(2);
            _tailList.Add(item);
        }
    }

    /// <inheritdoc cref="IList.Clear"/>
    public void Clear()
    {
        _firstItem = default!;
        if (_tailList is not EmptyList)
            _tailList?.Clear();
        _tailList = null;
    }

    /// <inheritdoc />
    public readonly bool Contains(T item)
    {
        var count = Count;
        if (count > 0 && ItemComparer.Equals(_firstItem, item))
            return true;
        return count > 1 && _tailList!.Contains(item);
    }

    /// <inheritdoc />
    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        // Formally (arrayIndex < array.GetLowerBound(0)) but since only SZArrays are allowed this is also OK.
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        var count = Count;

        if (count > array.Length - arrayIndex)
            throw new ArgumentException(nameof(arrayIndex));

        if (count > 0)
            array[arrayIndex++] = _firstItem;

        if (count <= 1)
            return;
        _tailList!.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    /// <inheritdoc />
    public readonly int IndexOf(T item)
    {
        var count = Count;
        switch (count)
        {
            case > 0 when ItemComparer.Equals(_firstItem, item):
                return 0;
            case > 1:
                {
                    var indexInList = _tailList!.IndexOf(item);
                    if (indexInList >= 0)
                        return 1 + indexInList;
                    break;
                }
        }
        return -1;
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        var count = Count;
        if ((uint)index > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index == 0)
        {
            switch (count)
            {
                case 0:
                    _tailList = EmptyDummyList;
                    break;
                case 1:
                    _tailList = new List<T>(2) { _firstItem };
                    break;
                default:
                    _tailList!.Insert(0, _firstItem);
                    break;
            }

            _firstItem = item;
        }
        else
        {
            if (_tailList == EmptyDummyList)
                _tailList = new List<T>(2);
            _tailList!.Insert(index - 1, item);
        }
    }

    /// <inheritdoc cref="IList.RemoveAt"/>
    public void RemoveAt(int index)
    {
        var count = Count;
        if ((uint)index >= (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index should be non-negative and less than Count.");
        if (index == 0)
        {
            if (count == 1)
            {
                _firstItem = default!;
                _tailList = null;
            }
            else
            {
                _firstItem = _tailList![0];
                if (count == 2)
                    _tailList = EmptyDummyList;
                else
                    _tailList.RemoveAt(0);
            }
        }
        else if (count == 2)
        {
            _tailList = EmptyDummyList;
        }
        else
        {
            _tailList!.RemoveAt(index - 1);
        }
    }

    #region Linq Re-Implemenations

    // Natively implementing frequent Linq functions avoids boxing. Add more if necessary.

    /// <summary>
    /// Creates a <see cref="List{T}"/> from an this instance.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> that contains elements from the this list.</returns>
    public readonly List<T> ToList()
    {
        if (_tailList is null)
            return [];
        var list = new List<T>(Count) { _firstItem };
        list.AddRange(_tailList);
        return list;
    }

    /// <summary>
    /// Copies the elements of the <see cref="FrugalList{T}"/> to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements of the <see cref="FrugalList{T}"/>.</returns>
    public readonly T[] ToArray()
    {
        var count = Count;
        if (count == 0)
            return [];
        var array = new T[count];
        CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Returns the first element of of the <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <returns>The first element of the specified <see cref="FrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="FrugalList{T}"/> is empty.</exception>
    public readonly T First()
    {
        if (Count == 0)
            throw new InvalidOperationException("The list contains no elements");
        return _firstItem;
    }

    /// <summary>
    /// Returns the last element of the <see cref="FrugalList{T}"/>.
    /// </summary>
    /// <returns>The last element of the specified <see cref="FrugalList{T}"/></returns>
    /// <exception cref="InvalidOperationException">The <see cref="FrugalList{T}"/> is empty.</exception>
    public readonly T Last()
    {
        var count = Count;
        if (count == 0)
            throw new InvalidOperationException("The list contains no elements");
        return count switch
        {
            1 => _firstItem,
            _ => _tailList![_tailList.Count - 1]
        };
    }

    /// <summary>
    /// Returns the first element of the <see cref="FrugalList{T}"/>, or a default value if no element is found.
    /// </summary>
    /// <returns><see langword="default(T)"/> if source is empty; otherwise, the first element in source.</returns>
    public readonly T? FirstOrDefault()
    {
        return _firstItem;
    }

    /// <summary>
    /// Returns the last element of the <see cref="FrugalList{T}"/>, or a default value if no element is found.
    /// </summary>
    /// <returns><see langword="default(T)"/> if source is empty; otherwise, the last element in source.</returns>
    public readonly T? LastOrDefault()
    {
        return Count switch
        {
            0 => default,
            1 => _firstItem,
            _ => _tailList![_tailList.Count - 1]
        };
    }

    #endregion

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="FrugalList{T}"/>
    /// </summary>
    /// <returns>A <see cref="FrugalEnumerator"/> for the <see cref="FrugalList{T}"/>.</returns>
    public FrugalEnumerator GetEnumerator() => new(ref this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new FrugalEnumerator(ref this);

    IEnumerator IEnumerable.GetEnumerator() => new FrugalEnumerator(ref this);

    /// <summary>
    /// Private type exists so that we can perform type checking on that type rather than reference checking.
    /// </summary>
    private sealed class EmptyList : List<T>
    {
        public static readonly EmptyList Instance = [];

        private EmptyList() : base(0)
        {
        }
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="FrugalList{T}"/>.
    /// </summary>
    public struct FrugalEnumerator : IEnumerator<T>
    {
        private readonly FrugalList<T> _list;

        private int _position;
        private T _current;

        readonly object IEnumerator.Current => Current!;

        /// <inheritdoc />
        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FrugalEnumerator(ref FrugalList<T> list)
        {
            _list = list;
            _position = 0;
            _current = default!;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_position < _list.Count)
            {
                _current = _list[_position];
                ++_position;
                return true;
            }
            _position = _list.Count + 1;
            _current = default!;
            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _current = default!;
            _position = 0;

        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}