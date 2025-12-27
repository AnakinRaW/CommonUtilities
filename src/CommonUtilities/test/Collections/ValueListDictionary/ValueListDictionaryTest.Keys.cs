using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Testing.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

// ReSharper disable once InconsistentNaming
public class ValueListDictionary_Keys : ICollectionTestSuite<string>
{
    protected override bool Enumerator_Empty_UsesSingletonInstance => false;
    protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => false;
    protected override bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => true;
    protected override bool NonGenericEnumerator_Current_UndefinedOperation_Throws => true;
    protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
    protected override bool DefaultValueAllowed => false;
    protected override bool DuplicateValuesAllowed => false;
    protected override bool IsReadOnly => true;
    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

    protected override ICollection<string> GenericICollectionFactory()
    {
        return new ValueListDictionary<string, string>().Keys;
    }

    protected override ICollection<string> GenericICollectionFactory(int count)
    {
        var list = new ValueListDictionary<string, string>();
        var seed = 13453;
        var random = new Random();
        for (var i = 0; i < count; i++)
        {
            var key = CreateT(seed++);
            list.Add(key, CreateT(seed++));
            while (random.Next() % 2 == 0) 
                list.Add(key, CreateT(seed++));
        }
        return list.Keys;
    }

    protected override string CreateT(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes = new byte[stringLength];
        rand.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    [Fact]
    public void ValueListDictionary_KeyCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new ValueListDictionary<string, string>.KeyCollection(null!));
    }

    [Theory]
    [MemberData(nameof(ValidCollectionSizes))]
    public void ValueListDictionary_KeyCollection_GetEnumerator(int count)
    {
        var dictionary = new ValueListDictionary<string, string>();
        var seed = 13453;
        while (dictionary.Count < count)
            dictionary.Add(CreateT(seed++), CreateT(seed++));
        using var _ = dictionary.Keys.GetEnumerator();
    }
}