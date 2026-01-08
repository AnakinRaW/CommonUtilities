using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

public abstract class IReadOnlyValueListDictionaryTestBase<TKey, TValue> : IEnumerableTestSuite<KeyValuePair<TKey, IReadOnlyList<TValue>>> 
    where TKey : notnull
{
    protected abstract bool DefaultValueAllowed { get; }

    // ReSharper disable once InconsistentNaming
    protected virtual bool ValueList_IsReadOnlyView => true;
    protected virtual bool IsReadOnly => true;

    protected sealed override bool Enumerator_Empty_UsesSingletonInstance => true;
    protected sealed override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
    protected sealed override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
    protected sealed override bool NonGenericEnumerator_Current_UndefinedOperation_Throws => true;
    protected sealed override bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => true;

    protected abstract TKey CreateTKey(int seed);

    protected abstract TValue CreateTValue(int seed);
    
    protected abstract IReadOnlyValueListDictionary<TKey, TValue> IReadOnlyValueListDictionaryFactory(int count);

    protected sealed override KeyValuePair<TKey, IReadOnlyList<TValue>> CreateT(int seed)
    {
        throw new NotSupportedException();
    }
    
    protected sealed override IEqualityComparer<KeyValuePair<TKey, IReadOnlyList<TValue>>> GetIEqualityComparer()
    {
        return new KVPComparer();
    }

    protected sealed override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        // ReSharper disable UseMethodAny.0
        if (IsReadOnly)
            yield break;

        if ((operations & ModifyOperation.Add) == ModifyOperation.Add)
        {
            yield return enumerable =>
            {
                var casted = (IValueListDictionary<TKey, TValue>)enumerable;
                casted.Add(CreateTKey(12), CreateTValue(5123));
                return true;
            };
        }
        if ((operations & ModifyOperation.Insert) == ModifyOperation.Insert)
        {
            yield return enumerable =>
            {
                var casted = (IValueListDictionary<TKey, TValue>)enumerable;
                casted.Add(CreateTKey(541), CreateTValue(12));
                return true;
            };
        }
        if ((operations & ModifyOperation.Remove) == ModifyOperation.Remove)
        {
            yield return enumerable =>
            {
                var casted = (IValueListDictionary<TKey, TValue>)enumerable;
                if (casted.Count() > 0)
                {
                    using var keys = casted.Keys.GetEnumerator();
                    keys.MoveNext();
                    casted.Remove(keys.Current!);
                    return true;
                }
                return false;
            };
        }
        if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
        {
            yield return enumerable =>
            {
                var casted = (IValueListDictionary<TKey, TValue>)enumerable;
                if (casted.Count() > 0)
                {
                    casted.Clear();
                    return true;
                }
                return false;
            };
        }
        //throw new InvalidOperationException(string.Format("{0:G}", operations));
        // ReSharper restore UseMethodAny.0
    }

    protected override IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>> GenericIEnumerableFactory(
        int count)
    {
        return IReadOnlyValueListDictionaryFactory(count);
    }

    protected void AddToCollection(IValueListDictionary<TKey, TValue> dictionary, int numberOfItemsToAdd)
    {
        var seed = 12353;
        var random = new Random();
        var initialCount = dictionary.KeyCount;
        while (dictionary.KeyCount - initialCount < numberOfItemsToAdd)
        {
            var toAdd = CreateTKey(seed++);
            while (dictionary.ContainsKey(toAdd))
                toAdd = CreateTKey(seed++);

            dictionary.Add(toAdd, CreateTValue(seed++));
            while (random.Next() % 2 == 0)
                dictionary.Add(toAdd, CreateTValue(seed++));
        }
    }

    protected TKey GetNewKey(IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    {
        var seed = 840;
        var missingKey = CreateTKey(seed++);
        while (dictionary.ContainsKey(missingKey) || missingKey.Equals(default(TKey)))
            missingKey = CreateTKey(seed++);
        return missingKey;
    }

    #region Item Getter

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ItemGet_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary[default!]);
            return;
        }

        if (!IsReadOnly)
        {
            var value = CreateTValue(3452);
            AddValue(dictionary, default!, value);
            Assert.Equal(value, dictionary[default!].First());
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ItemGet_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ItemGet_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ItemGet_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value, dictionary[pair.Key]);
    }

    #endregion

    #region Keys

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Keys_ContainsAllCorrectKeys(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var expected = dictionary.Select(pair => pair.Key);
        Assert.True(expected.SequenceEqual(dictionary.Keys));
    }
    
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Keys_IsReadOnly(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var keys = dictionary.Keys;
        Assert.True(keys.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => keys.Add(CreateTKey(11)));
        Assert.Throws<NotSupportedException>(() => keys.Clear());
        Assert.Throws<NotSupportedException>(() => keys.Remove(CreateTKey(11)));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Keys_Enumeration_Reset(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var keys = dictionary.Keys;
        using var enumerator = keys.GetEnumerator();
        enumerator.Reset();
    }

    #endregion

    #region Values

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Values_ContainsAllCorrectFlattenedValues(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var expected = dictionary.SelectMany(pair => pair.Value);
        Assert.True(expected.SequenceEqual(dictionary.Values));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Values_IsReadOnly(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var values = dictionary.Values;
        Assert.True(values.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => values.Add(CreateTValue(11)));
        Assert.Throws<NotSupportedException>(() => values.Clear());
        Assert.Throws<NotSupportedException>(() => values.Remove(CreateTValue(11)));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Values_Enumeration_Reset(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var values = dictionary.Values;
        using var enumerator = values.GetEnumerator();
        enumerator.Reset();
    }

    #endregion

    #region Count

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Count_Validity(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var expectedCount = dictionary.Sum(pair => pair.Value.Count);
        Assert.Equal(expectedCount, dictionary.Count);
    }

    #endregion

    #region KeyCount

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void KeyCount_Validity(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        Assert.Equal(count, dictionary.KeyCount);
    }

    #endregion

    #region ContainsKey

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ContainsKey_ValidKeyNotContainedInDictionary(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.ContainsKey(missingKey));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ContainsKey_ValidKeyContainedInDictionary(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (count > 0)
        {
            var key = dictionary.Keys.First();
            Assert.True(dictionary.ContainsKey(key));
        }

        if (!IsReadOnly)
        {
            var missingKey = GetNewKey(dictionary);
            AddValue(dictionary, missingKey, CreateTValue(34251));
            Assert.True(dictionary.ContainsKey(missingKey));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ContainsKey_DefaultKeyNotContainedInDictionary(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (DefaultValueAllowed)
        {
            if (!IsReadOnly)
            {
                // returns false
                var missingKey = default(TKey)!;
                while (dictionary.ContainsKey(missingKey))
                    RemoveKey(dictionary, missingKey);
                Assert.False(dictionary.ContainsKey(missingKey));
            }
        }
        else
        {
            // throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => dictionary.ContainsKey(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ContainsKey_DefaultKeyContainedInDictionary(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            if (!dictionary.ContainsKey(missingKey))
                AddValue(dictionary, missingKey, CreateTValue(5341));
            Assert.True(dictionary.ContainsKey(missingKey));
        }
    }

    #endregion

    #region GetValues

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetValues_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.GetValues(default!));
            return;
        }

        if (!IsReadOnly)
        {
            var value = CreateTValue(3452);
            AddValue(dictionary, default!, value);
            Assert.Equal(value, dictionary.GetValues(default!).First());
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetValues_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetValues_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetValues(missingKey));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetValues_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value, dictionary.GetValues(pair.Key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetValues_ReturnsReadOnlyViewOrSnapshot(int count)
    {
        if (IsReadOnly)
            return;

        var dict = IReadOnlyValueListDictionaryFactory(count);
        var key = GetNewKey(dict);
        var seed = 1234;
        AddValue(dict, key, CreateTValue(seed++));

        var values = dict.GetValues(key);

        var newValue = CreateTValue(seed);
        while (values.Contains(newValue))
            newValue = CreateTValue(++seed);

        AddValue(dict, key, newValue);

        // View reflects live changes, snapshot doesn't
        Assert.Equal(ValueList_IsReadOnlyView, values.Contains(newValue));

        // After removal, neither view nor snapshot contains the value
        RemoveValue(dict, key, newValue);
        Assert.DoesNotContain(newValue, values);

        // Removing key doesn't clear underlying list
        RemoveKey(dict, key);
        Assert.NotEmpty(values);

        // Clearing dict doesn't clear underlying lists
        if (count > 0)
        {
            var firstKey = dict.Keys.First();
            var firstValues = dict.GetValues(firstKey);
            ClearDict(dict);
            Assert.NotEmpty(firstValues);
        }
    }

    #endregion

    #region GetFirstValue

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetFirstValue_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.GetFirstValue(default!));
            return;
        }

        if (!IsReadOnly)
        {
            var first = CreateTValue(3452);
            var second = CreateTValue(4312);
            AddValue(dictionary, default!, first);
            AddValue(dictionary, default!, second);
            Assert.Equal(first, dictionary.GetFirstValue(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetFirstValue_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary.GetFirstValue(missingKey));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetFirstValue_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetFirstValue(missingKey));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetFirstValue_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value.First(), dictionary.GetFirstValue(pair.Key));
    }

    #endregion

    #region GetLastValue

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetLastValue_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.GetLastValue(default!));
            return;
        }

        if (!IsReadOnly)
        {
            var first = CreateTValue(3452);
            var second = CreateTValue(4312);
            AddValue(dictionary, default!, first);
            AddValue(dictionary, default!, second);
            Assert.Equal(second, dictionary.GetLastValue(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetLastValue_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.Throws<KeyNotFoundException>(() => dictionary.GetLastValue(missingKey));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetLastValue_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetLastValue(missingKey));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void GetLastValue_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value.Last(), dictionary.GetLastValue(pair.Key));
    }

    #endregion

    #region TryGetValues

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetValues_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.TryGetValues(default!, out _));
            return;
        }

        if (!IsReadOnly)
        {
            var first = CreateTValue(3452);
            var second = CreateTValue(5431);
            AddValue(dictionary, default!, first);
            AddValue(dictionary, default!, second);
            Assert.True(dictionary.TryGetValues(default!, out var valueList));
            Assert.Equal([first, second], valueList);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetValues_MissingNonDefaultKey_ReturnsFalseAndSetsDefault(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
        Assert.Equal([], valueList);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetValues_MissingDefaultKey_ReturnsFalseAndSetsDefault(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.False(dictionary.TryGetValues(missingKey, out var valueList));
            Assert.Equal([], valueList);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetValues_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
        {
            Assert.True(dictionary.TryGetValues(pair.Key, out var valueList));
            Assert.Equal(pair.Value, valueList);
        }
    }

    #endregion

    #region TryGetFirstValue

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetFirstValue_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.TryGetFirstValue(default!, out _));
            return;
        }

        if (!IsReadOnly)
        {
            var first = CreateTValue(3452);
            var second = CreateTValue(4312);
            AddValue(dictionary, default!, first);
            AddValue(dictionary, default!, second);

            Assert.True(dictionary.TryGetFirstValue(default!, out var value));
            Assert.Equal(first, value);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetFirstValue_MissingNonDefaultKey_ReturnsFalseAndSetsDefaultValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.TryGetFirstValue(missingKey, out var value));
        Assert.Equal(default, value);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetFirstValue_MissingDefaultKey_ReturnsFalseAndSetsDefaultValue(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.False(dictionary.TryGetFirstValue(missingKey, out var value));
            Assert.Equal(default, value);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetFirstValue_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
        {
            Assert.True(dictionary.TryGetFirstValue(pair.Key, out var value));
            Assert.Equal(pair.Value.First(), value);
        }
    }

    #endregion

    #region TryGetLastValue

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetLastValue_DefaultKey(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        if (!DefaultValueAllowed)
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.TryGetLastValue(default!, out _));
            return;
        }

        if (!IsReadOnly)
        {
            var first = CreateTValue(3452);
            var second = CreateTValue(4312);
            AddValue(dictionary, default!, first);
            AddValue(dictionary, default!, second);
            Assert.True(dictionary.TryGetLastValue(default!, out var value));
            Assert.Equal(second, value);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetLastValue_MissingNonDefaultKey_ReturnsFalseAndSetsDefaultValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.TryGetLastValue(missingKey, out var value));
        Assert.Equal(default, value);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetLastValue_MissingDefaultKey_ReturnsFalseAndSetsDefaultValue(int count)
    {
        if (DefaultValueAllowed && !IsReadOnly)
        {
            var dictionary = IReadOnlyValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                RemoveKey(dictionary, missingKey);
            Assert.False(dictionary.TryGetLastValue(missingKey, out var value));
            Assert.Equal(default, value);
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void TryGetLastValue_PresentKeyReturnsCorrectValue(int count)
    {
        var dictionary = IReadOnlyValueListDictionaryFactory(count);
        foreach (var pair in dictionary)
        {
            Assert.True(dictionary.TryGetLastValue(pair.Key, out var value));
            Assert.Equal(pair.Value.Last(), value);
        }
    }

    #endregion

    private void RemoveKey(IReadOnlyValueListDictionary<TKey, TValue> dictionary, TKey key)
    {
        if (IsReadOnly)
            throw new NotSupportedException("Test is read-only.");
        if (dictionary is not IValueListDictionary<TKey, TValue> mutable)
            throw new InvalidOperationException("Could not cast to mutable version");
        mutable.Remove(key);
    }
    
    private void AddValue(IReadOnlyValueListDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (IsReadOnly)
            throw new NotSupportedException("Test is read-only.");
        if (dictionary is not IValueListDictionary<TKey, TValue> mutable)
            throw new InvalidOperationException("Could not cast to mutable version");

        mutable.Add(key, value);
    }

    private void ClearDict(IReadOnlyValueListDictionary<TKey, TValue> dictionary)
    {
        if (IsReadOnly)
            throw new NotSupportedException("Test is read-only.");
        if (dictionary is not IValueListDictionary<TKey, TValue> mutable)
            throw new InvalidOperationException("Could not cast to mutable version");
        mutable.Clear();
    }

    private void RemoveValue(IReadOnlyValueListDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (IsReadOnly)
            throw new NotSupportedException("Test is read-only.");
        if (dictionary is not IValueListDictionary<TKey, TValue> mutable)
            throw new InvalidOperationException("Could not cast to mutable version");
        mutable.Remove(key, value);
    }

    // ReSharper disable once InconsistentNaming
    public class KVPComparer : IEqualityComparer<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    {
        public bool Equals(KeyValuePair<TKey, IReadOnlyList<TValue>> x, KeyValuePair<TKey, IReadOnlyList<TValue>> y)
        {
            if (!Equals(x.Key, y.Key))
                return false;
            
            if (x.Value.Count != y.Value.Count)
                return false;
            return !x.Value.Where((t, i) => !Equals(t, y.Value[i])).Any();
        }

        public int GetHashCode(KeyValuePair<TKey, IReadOnlyList<TValue>> obj)
        {
            var hashCode = new HashCode();

            hashCode.Add(obj.Key);
            foreach (var item in obj.Value)
                hashCode.Add(item);
            return hashCode.ToHashCode();
        }
    }
}