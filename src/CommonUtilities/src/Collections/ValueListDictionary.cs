using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a generic dictionary that maps keys to one or more values,
/// while maintaining the order of key insertion.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the lists associated with the keys.</typeparam>
/// <remarks>
/// <para>
/// Unlike a standard <see cref="Dictionary{TKey, TValue}"/>, this dictionary allows multiple values 
/// to be associated with a single key. Values are stored in the order they were added.
/// </para>
/// <para>
/// A <see cref="ValueListDictionary{TKey,TValue}"/> can support multiple readers concurrently, 
/// as long as the collection is not modified. Even so, enumerating through a collection is 
/// intrinsically not a thread-safe procedure. In the rare case where an enumeration contends 
/// with write accesses, the collection must be locked during the entire enumeration.
/// </para>
/// </remarks>
[DebuggerTypeProxy(typeof(IValueListDictionaryDebugView<,>))]
[DebuggerDisplay("ValueCount = {ValueCount}")]
public class ValueListDictionary<TKey, TValue> 
    : ValueListDictionaryBase<TKey, TValue, IList<TValue>>  
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionary{TKey, TValue}"/> class
    /// that is empty and uses the default equality comparer for the key type.
    /// </summary>
    public ValueListDictionary()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueListDictionary{TKey, TValue}"/> class
    /// that is empty and uses the specified <see cref="IEqualityComparer{T}"/>
    /// </summary>
    /// <param name="equalityComparer">
    /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys,
    /// or <see langword="null"/> to use the default <see cref="IEqualityComparer{T}"/> for the type of the key.
    /// </param>
    /// <remarks>
    /// This constructor allows customization of how keys are compared in the dictionary. 
    /// If no equality comparer is provided, the default comparer for the key type is used.
    /// </remarks>
    public ValueListDictionary(IEqualityComparer<TKey>? equalityComparer) : base(equalityComparer)
    {
        
    }
    
    /// <summary>
    /// Creates a new instance of the value store for the dictionary.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="IList{T}"/> to be used as the value store for the dictionary.
    /// </returns>
    protected override IList<TValue> CreateValueStore()
    {
        return new ReadOnlyCachingList();
    }

    /// <summary>
    /// Creates a snapshot of the specified list, providing a read-only view of its current state.
    /// </summary>
    /// <param name="list">The list from which to create the snapshot.</param>
    /// <returns>A read-only view of the specified list.</returns>
    protected override IReadOnlyList<TValue> CreateSnapshot(IList<TValue> list)
    {
        return ((ReadOnlyCachingList)list).GetReadOnlyView();
    }

    /// <summary>
    /// Invoked after a value list associated with a specific key has been modified.
    /// </summary>
    /// <remarks>
    /// This method invalidates the cached read-only view of <paramref name="list"/>.
    /// </remarks>
    /// <param name="key">
    /// The key associated with the modified value list.
    /// </param>
    /// <param name="list">
    /// The list of values that has been modified.
    /// </param>
    protected override void OnAfterValueListModified(TKey key, IList<TValue> list)
    {
        ((ReadOnlyCachingList)list).InvalidateCache();
    }
    
    /// <summary>
    /// Represents a specialized list that supports caching of its read-only view.
    /// </summary>
    /// <remarks>
    /// This class is used internally by <see cref="ValueListDictionary{TKey,TValue}"/> to manage 
    /// the storage of values associated with a key. It provides efficient caching of a read-only 
    /// view of the list to minimize redundant allocations and improve performance.
    /// </remarks>
    internal sealed class ReadOnlyCachingList : List<TValue>
    {
        private ReadOnlyCollection<TValue>? _cachedReadOnly;
        
        internal ReadOnlyCollection<TValue> GetReadOnlyView()
        {
            return _cachedReadOnly ??= new ReadOnlyCollection<TValue>(this);
        }

        internal void InvalidateCache()
        {
            _cachedReadOnly = null;
        }
    }
}

