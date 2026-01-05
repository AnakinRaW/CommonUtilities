//using System;
//using System.Collections.Generic;
//using AnakinRaW.CommonUtilities.Collections;
//using AnakinRaW.CommonUtilities.Testing.Collections;
//using Xunit;

//namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

//// ReSharper disable once InconsistentNaming
//public class ReadOnlyValueListDictionary_Values : ICollectionTestSuite<string>
//{
//    protected override bool DefaultValueAllowed => true;
//    protected override bool DuplicateValuesAllowed => true;
//    protected override bool IsReadOnly => true;
//    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();
//    protected override bool Enumerator_Empty_UsesSingletonInstance => true;
//    protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
//    protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;

//    protected override bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => true;
//    protected override bool NonGenericEnumerator_Current_UndefinedOperation_Throws => true;

//    protected override ICollection<string> GenericICollectionFactory()
//    {
//        return new ReadOnlyValueListDictionary<string, string>(new ValueListDictionary<string, string>()).Values;
//    }

//    protected override ICollection<string> GenericICollectionFactory(int count)
//    {
//        var list = new ValueListDictionary<string, string>();
//        var seed = 13453;
//        var random = new Random(seed);

//        var valuesAdded = 0;
//        while (valuesAdded < count)
//        {
//            var key = CreateT(seed++);

//            // Add first value for this key
//            list.Add(key, CreateT(seed++));
//            valuesAdded++;

//            // Randomly add more values for the same key, but don't exceed count
//            while (valuesAdded < count && random.Next() % 2 == 0)
//            {
//                list.Add(key, CreateT(seed++));
//                valuesAdded++;
//            }
//        }

//        return new ReadOnlyValueListDictionary<string, string>(list).Values;
//    }

//    protected override string CreateT(int seed)
//    {
//        var stringLength = seed % 10 + 5;
//        var rand = new Random(seed);
//        var bytes = new byte[stringLength];
//        rand.NextBytes(bytes);
//        return Convert.ToBase64String(bytes);
//    }

//    [Fact]
//    public void ValueListDictionary_ValueCollection_Constructor_NullDictionary()
//    {
//        Assert.Throws<ArgumentNullException>(() => new ReadOnlyValueListDictionary<string, string>.ValueCollection(null!));
//    }

//    [Theory]
//    [MemberData(nameof(ValidCollectionSizes))]
//    public void ValueListDictionary_ValueCollection_GetEnumerator(int count)
//    {
//        var dictionary = new ValueListDictionary<string, string>();
//        var seed = 13453;
//        while (dictionary.Count < count)
//            dictionary.Add(CreateT(seed++), CreateT(seed++));
//        using var _ = new ReadOnlyValueListDictionary<string, string>(dictionary).Values.GetEnumerator();
//    }
//}