using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Collections;

internal sealed class IValueListDictionaryDebugView<TKey, TValue>(IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    where TKey : notnull
{
    private readonly IReadOnlyValueListDictionary<TKey, TValue> _dict = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DebugViewValueListDictionaryItem<TKey, TValue>[] Items => 
        _dict.Select(keyValuePair => new DebugViewValueListDictionaryItem<TKey, TValue>(keyValuePair))
            .ToArray();
}

[DebuggerDisplay("{ValueList}", Name = "[{Key}]")]
internal readonly struct DebugViewValueListDictionaryItem<TKey, TValue>(KeyValuePair<TKey, IReadOnlyList<TValue>> keyValue)
{
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public TKey Key { get; } = keyValue.Key;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<TValue> ValueList { get; } = keyValue.Value;
}


// From https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ICollectionDebugView.cs
internal sealed class IReadOnlyCollectionDebugView<T>(IReadOnlyCollection<T> collection)
{
    private readonly IReadOnlyCollection<T> _collection = collection ?? throw new ArgumentNullException(nameof(collection));

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _collection.ToArray();
}

internal sealed class ICollectionDebugView<T>(ICollection<T> collection)
{
    private readonly ICollection<T> _collection = collection ?? throw new ArgumentNullException(nameof(collection));

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var items = new T[_collection.Count];
            _collection.CopyTo(items, 0);
            return items;
        }
    }
}