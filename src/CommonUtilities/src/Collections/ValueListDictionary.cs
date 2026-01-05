using System.Collections.Generic;
using System.Diagnostics;


namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a generic dictionary that maps keys to one or more values, while maintaining the order of key insertion.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
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
[DebuggerDisplay("Count = {Count}")]
public class ValueListDictionary<TKey, TValue> : ValueListDictionaryBase<TKey, TValue, List<TValue>>  where TKey : notnull
{
    public ValueListDictionary()
    {
    }

    public ValueListDictionary(IEqualityComparer<TKey>? equalityComparer) : base(equalityComparer)
    {
        
    }
    
    protected override List<TValue> CreateValueStore()
    {
        return [];
    }
}