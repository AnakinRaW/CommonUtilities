using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.EqualityComparers;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class IValueListDictionaryTestBase<TKey, TValue> : IReadOnlyValueListDictionaryTestBase<TKey, TValue> 
    where TKey : notnull
{
    protected override bool IsReadOnly => false;

    // ReSharper disable once InconsistentNaming
    protected bool Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified => true;

    protected abstract IValueListDictionary<TKey, TValue> IValueListDictionaryFactory(IEqualityComparer<TKey>? comparer = null);

    protected virtual IValueListDictionary<TKey, TValue> IValueListDictionaryFactory(int count)
    {
        var collection = IValueListDictionaryFactory();
        AddToCollection(collection, count);
        return collection;
    }

    protected override IReadOnlyValueListDictionary<TKey, TValue> IReadOnlyValueListDictionaryFactory(int count)
    {
        return IValueListDictionaryFactory(count);
    }
    
    #region Keys

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Keys_ModifyingTheDictionaryUpdatesTheCollection(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var keys = dictionary.Keys;
        if (count > 0)
            Assert.NotEmpty(keys);
        dictionary.Clear();
        Assert.Empty(keys);
       
    }
    
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Keys_Enumeration_ParentDictionaryModifiedInvalidates(int count)
    {
        if (!IsReadOnly)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var keys = dictionary.Keys;
            using var keysEnum = keys.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified)
            {
                Assert.Throws<InvalidOperationException>(() => keysEnum.MoveNext());
                Assert.Throws<InvalidOperationException>(() => keysEnum.Reset());
            }
            else
            {
                if (keysEnum.MoveNext())
                {
                    _ = keysEnum.Current;
                }
                keysEnum.Reset();
            }
        }
    }

    #endregion

    #region Values

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Values_Enumeration_ParentDictionaryModifiedInvalidates(int count)
    {
        if (!IsReadOnly)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var values = dictionary.Values;
            using var valuesEnum = values.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            if (count == 0 ? Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException : Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified)
            {
                Assert.Throws<InvalidOperationException>(() => valuesEnum.MoveNext());
                Assert.Throws<InvalidOperationException>(() => valuesEnum.Reset());
            }
            else
            {
                if (valuesEnum.MoveNext())
                {
                    _ = valuesEnum.Current;
                }
                valuesEnum.Reset();
            }
        }
    }


    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Values_IncludeDuplicatesMultipleTimes(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var oldValueCount = dictionary.ValueCount;
        var oldKeyCount = dictionary.KeyCount;
        var seed = 431;
        foreach (var pair in dictionary.ToList())
        {
            var missingKey = CreateTKey(seed++);
            while (dictionary.ContainsKey(missingKey))
                missingKey = CreateTKey(seed++);
            dictionary.Add(missingKey, pair.Value.First());
        }
        Assert.Equal(oldValueCount + oldKeyCount, dictionary.Values.Count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Values_ModifyingTheDictionaryUpdatesTheCollection(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var values = dictionary.Values;
        if (count > 0)
            Assert.NotEmpty(values);

        dictionary.Clear();
        Assert.Empty(values);
    }

    #endregion

    #region Add(TKey, TValue)
    
    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_DefaultKey_DefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.ValueCount;
        var missingKey = default(TKey)!;
        var value = default(TValue)!;
        if (DefaultValueAllowed)
        {
            Assert.False(dictionary.Add(missingKey, value));
            Assert.Equal(valueCoutBeforeAdd + 1, dictionary.ValueCount);
            Assert.Equal(count + 1, dictionary.KeyCount);
            Assert.Equal(value, dictionary[missingKey].First());
            Assert.Equal(value, dictionary[missingKey].Last());
        }
        else
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, value));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_DefaultKey_NonDefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.ValueCount;
        var missingKey = default(TKey)!;
        var value = CreateTValue(1456);
        if (DefaultValueAllowed)
        {
            Assert.False(dictionary.Add(missingKey, value));
            Assert.Equal(valueCoutBeforeAdd + 1, dictionary.ValueCount);
            Assert.Equal(count + 1, dictionary.KeyCount);
            Assert.Equal(value, dictionary[missingKey].First());
            Assert.Equal(value, dictionary[missingKey].Last());
        }
        else
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, value));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_NonDefaultKey_DefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.ValueCount;
        var missingKey = GetNewKey(dictionary);
        var value = default(TValue)!;
        Assert.False(dictionary.Add(missingKey, value));
        Assert.Equal(valueCoutBeforeAdd + 1, dictionary.ValueCount);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(value, dictionary[missingKey].First());
        Assert.Equal(value, dictionary[missingKey].Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_NonDefaultKey_NonDefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.ValueCount;
        var missingKey = GetNewKey(dictionary);
        var value = CreateTValue(1342);
        Assert.False(dictionary.Add(missingKey, value));
        Assert.Equal(valueCoutBeforeAdd + 1, dictionary.ValueCount);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(value, dictionary[missingKey].First());
        Assert.Equal(value, dictionary[missingKey].Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_DuplicateValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var seed = 321;
        var duplicate = CreateTValue(seed++);
        while (dictionary.Values.Contains(duplicate))
            duplicate = CreateTValue(seed++);
        Assert.False(dictionary.Add(GetNewKey(dictionary), duplicate));
        Assert.False(dictionary.Add(GetNewKey(dictionary), duplicate));
        Assert.Equal(2, dictionary.Values.Count(value => value!.Equals(duplicate)));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_DuplicateKey_AddsToList(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var duplicateKey = GetNewKey(dictionary);
        Assert.False(dictionary.Add(duplicateKey, CreateTValue(34251)));
        Assert.Single(dictionary[duplicateKey]);
        var valueCountBeforeSecondAdd = dictionary.ValueCount;
        var keyCountBeforeSecondAdd = dictionary.KeyCount;

        Assert.True(dictionary.Add(duplicateKey, CreateTValue(134)));
        Assert.Equal(2, dictionary[duplicateKey].Count);
        Assert.Equal(keyCountBeforeSecondAdd, dictionary.KeyCount);
        Assert.Equal(valueCountBeforeSecondAdd + 1, dictionary.ValueCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Add_DistinctValuesWithHashCollisions(int count)
    {
        var dictionary = IValueListDictionaryFactory(new ConstantHashCodeEqualityComparer<TKey>(EqualityComparer<TKey>.Default));
        AddToCollection(dictionary, count);
        Assert.Equal(count, dictionary.KeyCount);
    }

    #endregion

    #region Remove(TKey)

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Remove_EveryKey(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        Assert.All(dictionary.Keys.ToList(), key =>
        {
            Assert.True(dictionary.Remove(key));
        });
        Assert.Empty(dictionary);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Remove_ValidKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.Remove(missingKey));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueList_Dictionary_Remove_ValidKeyContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        dictionary.Add(missingKey, CreateTValue(34251));
        Assert.True(dictionary.Remove(missingKey));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Remove_DefaultKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        if (DefaultValueAllowed)
        {
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                dictionary.Remove(missingKey);
            Assert.False(dictionary.Remove(missingKey));
        }
        else
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.Remove(default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Remove_DefaultKeyContainedInDictionary(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            dictionary.Add(missingKey, CreateTValue(5341));
            Assert.True(dictionary.Remove(missingKey));
        }
    }

    #endregion

    #region Remove(TKey, TValue)

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_Everything(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        Assert.All(dictionary.Keys.ToList(), key =>
        {
            foreach (var value in dictionary.GetValues(key).ToList()) 
                Assert.True(dictionary.Remove(key, value));
        });
        Assert.Empty(dictionary);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueList_Dictionary_RemoveKeyValue_ValidKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.Remove(missingKey, default!));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_ValidKeyContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        var value = CreateTValue(34251);
        dictionary.Add(missingKey, value);
        Assert.True(dictionary.Remove(missingKey, value));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_ValidKeyContainedInDictionary_ValueNotContained(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        var seed = 34251;
        var value = CreateTValue(seed++)!;
        
        var missingValue = CreateTValue(seed++);
        while (value.Equals(missingValue))
            missingValue = CreateTValue(seed++);

        dictionary.Add(missingKey, value);
        Assert.False(dictionary.Remove(missingKey, missingValue));
        Assert.Equal(count + 1, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_ValidKeyContainedInDictionary_DuplicateValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        var value = CreateTValue(34251);
        
        dictionary.Add(missingKey, value);
        dictionary.Add(missingKey, value);

        Assert.True(dictionary.Remove(missingKey, value));
        Assert.Equal([value], dictionary.GetValues(missingKey));
        Assert.Equal(count + 1, dictionary.KeyCount);
    }


    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_DefaultKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        if (DefaultValueAllowed)
        {
            var missingKey = default(TKey)!;
            while (dictionary.ContainsKey(missingKey))
                dictionary.Remove(missingKey);
            Assert.False(dictionary.Remove(missingKey, default!));
        }
        else
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.Remove(default!, default!));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveKeyValue_DefaultKeyContainedInDictionary(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var missingKey = default(TKey)!;
            var value = CreateTValue(5341);
            dictionary.Add(missingKey, value);
            Assert.True(dictionary.Remove(missingKey, value));
        }
    }

    #endregion

    #region Clear

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Clear(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        dictionary.Clear();
        Assert.Equal(0, dictionary.ValueCount);
        Assert.Equal(0, dictionary.KeyCount);
        Assert.Empty(dictionary.Keys);
        Assert.Empty(dictionary.Values);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_Clear_Repeatedly(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        dictionary.Clear();
        dictionary.Clear();
        dictionary.Clear();
        Assert.Equal(0, dictionary.ValueCount);
        Assert.Equal(0, dictionary.KeyCount);
        Assert.Empty(dictionary.Keys);
        Assert.Empty(dictionary.Values);
    }

    #endregion

    #region AddRange(TKey, IEnumerable<TValue>)

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_NullKey_ThrowsArgumentNullException(int count)
    {
        if (!DefaultValueAllowed)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var values = new[] { CreateTValue(1), CreateTValue(2) };
            Assert.Throws<ArgumentNullException>(() => dictionary.AddRange(default!, values));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_NullValues_ThrowsArgumentNullException(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        Assert.Throws<ArgumentNullException>(() => dictionary.AddRange(key, null!));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_EmptyEnumerable_DoesNotModifyDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var valueCountBefore = dictionary.ValueCount;
        var keyCountBefore = dictionary.KeyCount;

        dictionary.AddRange(key, []);

        Assert.Equal(valueCountBefore, dictionary.ValueCount);
        Assert.Equal(keyCountBefore, dictionary.KeyCount);
        Assert.False(dictionary.ContainsKey(key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_NewKey_MultipleValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var values = new[] { CreateTValue(1), CreateTValue(2), CreateTValue(3) };
        var valueCountBefore = dictionary.ValueCount;

        dictionary.AddRange(key, values);

        Assert.Equal(valueCountBefore + 3, dictionary.ValueCount);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(values, dictionary.GetValues(key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_ExistingKey_MultipleValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var initialValue = CreateTValue(100);
        dictionary.Add(key, initialValue);

        var valueCountBefore = dictionary.ValueCount;
        var keyCountBefore = dictionary.KeyCount;
        var values = new[] { CreateTValue(1), CreateTValue(2), CreateTValue(3) };

        dictionary.AddRange(key, values);

        Assert.Equal(valueCountBefore + 3, dictionary.ValueCount);
        Assert.Equal(keyCountBefore, dictionary.KeyCount);
        var expectedValues = new[] { initialValue }.Concat(values).ToArray();
        Assert.Equal(expectedValues, dictionary.GetValues(key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_DefaultKey_MultipleValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = default(TKey)!;
        var values = new[] { CreateTValue(1), CreateTValue(2) };

        if (DefaultValueAllowed)
        {
            var valueCountBefore = dictionary.ValueCount;
            dictionary.AddRange(key, values);
            Assert.Equal(valueCountBefore + 2, dictionary.ValueCount);
            Assert.Equal(values, dictionary.GetValues(key));
        }
        else
        {
            Assert.Throws<ArgumentNullException>(() => dictionary.AddRange(key, values));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_AddRange_SingleValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var values = new[] { CreateTValue(1) };
        var valueCountBefore = dictionary.ValueCount;

        dictionary.AddRange(key, values);

        Assert.Equal(valueCountBefore + 1, dictionary.ValueCount);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(values, dictionary.GetValues(key));
    }

    #endregion

    #region RemoveAll(TKey, Predicate<TValue>)

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_NullKey_ThrowsArgumentNullException(int count)
    {
        if (!DefaultValueAllowed)
        {
            var dictionary = IValueListDictionaryFactory(count);
            Assert.Throws<ArgumentNullException>(() => dictionary.RemoveAll(default!, _ => true));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_NullPredicate_ThrowsArgumentNullException(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        Assert.Throws<ArgumentNullException>(() => dictionary.RemoveAll(key, null!));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_KeyNotInDictionary_ReturnsZero(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var removed = dictionary.RemoveAll(key, _ => true);
        Assert.Equal(0, removed);
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_RemoveSomeValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        dictionary.Add(key, CreateTValue(1));
        dictionary.Add(key, CreateTValue(2));
        dictionary.Add(key, CreateTValue(3));
        dictionary.Add(key, CreateTValue(4));

        var valueCountBefore = dictionary.ValueCount;
        var keyCountBefore = dictionary.KeyCount;

        var removed = dictionary.RemoveAll(key, v => v!.Equals(CreateTValue(2)) || v.Equals(CreateTValue(4)));

        Assert.Equal(2, removed);
        Assert.Equal(valueCountBefore - 2, dictionary.ValueCount);
        Assert.Equal(keyCountBefore, dictionary.KeyCount);
        Assert.Equal(2, dictionary.GetValues(key).Count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_RemoveAllValues_RemovesKey(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        dictionary.Add(key, CreateTValue(1));
        dictionary.Add(key, CreateTValue(2));
        dictionary.Add(key, CreateTValue(3));

        var valueCountBefore = dictionary.ValueCount;
        var keyCountBefore = dictionary.KeyCount;

        var removed = dictionary.RemoveAll(key, _ => true);

        Assert.Equal(3, removed);
        Assert.Equal(valueCountBefore - 3, dictionary.ValueCount);
        Assert.Equal(keyCountBefore - 1, dictionary.KeyCount);
        Assert.False(dictionary.ContainsKey(key));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_PredicateMatchesNothing_ReturnsZero(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        dictionary.Add(key, CreateTValue(1));
        dictionary.Add(key, CreateTValue(2));

        var valueCountBefore = dictionary.ValueCount;
        var keyCountBefore = dictionary.KeyCount;

        var removed = dictionary.RemoveAll(key, _ => false);

        Assert.Equal(0, removed);
        Assert.Equal(valueCountBefore, dictionary.ValueCount);
        Assert.Equal(keyCountBefore, dictionary.KeyCount);
        Assert.Equal(2, dictionary.GetValues(key).Count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_DefaultKey(int count)
    {
        if (DefaultValueAllowed)
        {
            var dictionary = IValueListDictionaryFactory(count);
            var key = default(TKey)!;
            dictionary.Add(key, CreateTValue(1));
            dictionary.Add(key, CreateTValue(2));

            var removed = dictionary.RemoveAll(key, v => v!.Equals(CreateTValue(1)));

            Assert.Equal(1, removed);
            Assert.Single(dictionary.GetValues(key));
        }
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void IValueListDictionary_RemoveAll_RemovesOnlyMatchingValues(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var key = GetNewKey(dictionary);
        var value1 = CreateTValue(1);
        var value2 = CreateTValue(2);
        var value3 = CreateTValue(3);

        dictionary.Add(key, value1);
        dictionary.Add(key, value2);
        dictionary.Add(key, value3);
        dictionary.Add(key, value1);

        var removed = dictionary.RemoveAll(key, v => v!.Equals(value1));

        Assert.Equal(2, removed);
        Assert.Equal(2, dictionary.GetValues(key).Count);
        Assert.All(dictionary.GetValues(key), v => Assert.NotEqual(value1, v));
    }

    #endregion
}