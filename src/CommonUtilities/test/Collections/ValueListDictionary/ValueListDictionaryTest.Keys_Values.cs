using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Collections;
using System;
using System.Collections.Generic;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public abstract class ValueListDictionary_Keys_Values_CollectionTestSuite : ICollectionTestSuite<string>
{
    public enum CollectionSelector
    {
        Keys,
        Values
    }
    protected abstract CollectionSelector Selector { get; }

    protected sealed override bool IsReadOnly => true;
    protected sealed override bool Enumerator_Empty_UsesSingletonInstance => true;
    protected sealed override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
    protected sealed override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
    protected sealed override bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => true;
    protected sealed override bool NonGenericEnumerator_Current_UndefinedOperation_Throws => true;

    protected sealed override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

    protected sealed override ICollection<string> GenericICollectionFactory()
    {
        var dictionary = ValueListDictionaryFactory();
        return Selector is CollectionSelector.Keys
            ? dictionary.Keys
            : dictionary.Values;
    }

    protected sealed override ICollection<string> GenericICollectionFactory(int count)
    {
        var mutableDictionary = MutableValueListDictionaryFactory();
        Populate(mutableDictionary, count);

        var dictionary = ValueListDictionaryFactory(mutableDictionary);
        return Selector is CollectionSelector.Keys
            ? dictionary.Keys
            : dictionary.Values;
    }

    protected sealed override string CreateT(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes = new byte[stringLength];
        rand.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    protected abstract IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory();

    protected abstract IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory(
        IValueListDictionary<string, string> dictionary);

    protected abstract void Populate(IValueListDictionary<string, string> dictionary, int count);

    protected virtual IValueListDictionary<string, string> MutableValueListDictionaryFactory()
    {
        return new ValueListDictionary<string, string>();
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ValueListDictionary_KeyOrValueCollection_GetEnumerator(int count)
    {
        var mutable = MutableValueListDictionaryFactory();
        var seed = 13453;
        while (mutable.Count < count)
            mutable.Add(CreateT(seed++), CreateT(seed++));
        var dictionary = ValueListDictionaryFactory(mutable);
        if (Selector is CollectionSelector.Keys)
        {
            using var _ = dictionary.Keys.GetEnumerator();
        }
        else
        {
            using var _ = dictionary.Values.GetEnumerator();
        }
    }
}

public abstract class ValueListDictionary_Keys_TestSuite : ValueListDictionary_Keys_Values_CollectionTestSuite
{
    protected sealed override CollectionSelector Selector => CollectionSelector.Keys;
    protected sealed override bool DefaultValueAllowed => false;
    protected sealed override bool DuplicateValuesAllowed => false;

    protected sealed override void Populate(IValueListDictionary<string, string> dictionary, int count)
    {
        var seed = 13453;
        var random = new Random();
        for (var i = 0; i < count; i++)
        {
            var key = CreateT(seed++);
            dictionary.Add(key, CreateT(seed++));
            while (random.Next() % 2 == 0)
                dictionary.Add(key, CreateT(seed++));
        }
    }
}

public abstract class ValueListDictionary_Values_TestSuite : ValueListDictionary_Keys_Values_CollectionTestSuite
{
    protected sealed override CollectionSelector Selector => CollectionSelector.Values;

    protected sealed override bool DefaultValueAllowed => true;
    protected sealed override bool DuplicateValuesAllowed => true;
    
    protected override void Populate(IValueListDictionary<string, string> dictionary, int count)
    {
        var seed = 13453;
        var random = new Random(seed);

        var valuesAdded = 0;
        while (valuesAdded < count)
        {
            var key = CreateT(seed++);

            // Add first value for this key
            dictionary.Add(key, CreateT(seed++));
            valuesAdded++;

            // Randomly add more values for the same key, but don't exceed count
            while (valuesAdded < count && random.Next() % 2 == 0)
            {
                dictionary.Add(key, CreateT(seed++));
                valuesAdded++;
            }
        }
    }
}