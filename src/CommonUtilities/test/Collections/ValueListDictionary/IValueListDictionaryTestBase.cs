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
    public void Keys_ModifyingTheDictionaryUpdatesTheCollection(int count)
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
    public void Keys_Enumeration_ParentDictionaryModifiedInvalidates(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var keys = dictionary.Keys;
        using var keysEnum = keys.GetEnumerator();
        dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));

        Assert.Throws<InvalidOperationException>(() => keysEnum.MoveNext());
        Assert.Throws<InvalidOperationException>(() => keysEnum.Reset());
    }

    #endregion

    #region Values

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Values_Enumeration_ParentDictionaryModifiedInvalidates(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var values = dictionary.Values;
        using var valuesEnum = values.GetEnumerator();
        dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
        if (valuesEnum.MoveNext())
        {
            _ = valuesEnum.Current;
        }
        valuesEnum.Reset();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Values_IncludeDuplicatesMultipleTimes(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var oldValueCount = dictionary.Count;
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
    public void Values_ModifyingTheDictionaryUpdatesTheCollection(int count)
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
    public void Add_DefaultKey_DefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.Count;
        var missingKey = default(TKey)!;
        var value = default(TValue)!;
        if (DefaultValueAllowed)
        {
            Assert.False(dictionary.Add(missingKey, value));
            Assert.Equal(valueCoutBeforeAdd + 1, dictionary.Count);
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
    public void Add_DefaultKey_NonDefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.Count;
        var missingKey = default(TKey)!;
        var value = CreateTValue(1456);
        if (DefaultValueAllowed)
        {
            Assert.False(dictionary.Add(missingKey, value));
            Assert.Equal(valueCoutBeforeAdd + 1, dictionary.Count);
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
    public void Add_NonDefaultKey_DefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.Count;
        var missingKey = GetNewKey(dictionary);
        var value = default(TValue)!;
        Assert.False(dictionary.Add(missingKey, value));
        Assert.Equal(valueCoutBeforeAdd + 1, dictionary.Count);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(value, dictionary[missingKey].First());
        Assert.Equal(value, dictionary[missingKey].Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Add_NonDefaultKey_NonDefaultValue(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var valueCoutBeforeAdd = dictionary.Count;
        var missingKey = GetNewKey(dictionary);
        var value = CreateTValue(1342);
        Assert.False(dictionary.Add(missingKey, value));
        Assert.Equal(valueCoutBeforeAdd + 1, dictionary.Count);
        Assert.Equal(count + 1, dictionary.KeyCount);
        Assert.Equal(value, dictionary[missingKey].First());
        Assert.Equal(value, dictionary[missingKey].Last());
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Add_DuplicateValue(int count)
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
    public void Add_DuplicateKey_AddsToList(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var duplicateKey = GetNewKey(dictionary);
        Assert.False(dictionary.Add(duplicateKey, CreateTValue(34251)));
        Assert.Single(dictionary[duplicateKey]);
        var valueCountBeforeSecondAdd = dictionary.Count;
        var keyCountBeforeSecondAdd = dictionary.KeyCount;

        Assert.True(dictionary.Add(duplicateKey, CreateTValue(134)));
        Assert.Equal(2, dictionary[duplicateKey].Count);
        Assert.Equal(keyCountBeforeSecondAdd, dictionary.KeyCount);
        Assert.Equal(valueCountBeforeSecondAdd + 1, dictionary.Count);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Add_DistinctValuesWithHashCollisions(int count)
    {
        var dictionary = IValueListDictionaryFactory(new ConstantHashCodeEqualityComparer<TKey>(EqualityComparer<TKey>.Default));
        AddToCollection(dictionary, count);
        Assert.Equal(count, dictionary.KeyCount);
    }

    #endregion

    #region Remove(TKey)

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Remove_EveryKey(int count)
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
    public void Remove_ValidKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.Remove(missingKey));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Remove_ValidKeyContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        dictionary.Add(missingKey, CreateTValue(34251));
        Assert.True(dictionary.Remove(missingKey));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void Remove_DefaultKeyNotContainedInDictionary(int count)
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
    public void Remove_DefaultKeyContainedInDictionary(int count)
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
    public void RemoveKeyValue_Everything(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        Assert.All(dictionary.Keys.ToList(), key =>
        {
            foreach (var value in dictionary.GetValues(key)) 
                Assert.True(dictionary.Remove(key, value));
        });
        Assert.Empty(dictionary);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void RemoveKeyValue_ValidKeyNotContainedInDictionary(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        var missingKey = GetNewKey(dictionary);
        Assert.False(dictionary.Remove(missingKey, default!));
        Assert.Equal(count, dictionary.KeyCount);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void RemoveKeyValue_ValidKeyContainedInDictionary(int count)
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
    public void RemoveKeyValue_ValidKeyContainedInDictionary_ValueNotContained(int count)
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
    public void RemoveKeyValue_ValidKeyContainedInDictionary_DuplicateValues(int count)
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
    public void RemoveKeyValue_DefaultKeyNotContainedInDictionary(int count)
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
    public void RemoveKeyValue_DefaultKeyContainedInDictionary(int count)
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
    public void ICollection_Generic_Clear(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        dictionary.Clear();
        Assert.Equal(0, dictionary.Count);
        Assert.Equal(0, dictionary.KeyCount);
        Assert.Empty(dictionary.Keys);
        Assert.Empty(dictionary.Values);
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ICollection_Generic_Clear_Repeatedly(int count)
    {
        var dictionary = IValueListDictionaryFactory(count);
        dictionary.Clear();
        dictionary.Clear();
        dictionary.Clear();
        Assert.Equal(0, dictionary.Count);
        Assert.Equal(0, dictionary.KeyCount);
        Assert.Empty(dictionary.Keys);
        Assert.Empty(dictionary.Values);
    }

    #endregion
}