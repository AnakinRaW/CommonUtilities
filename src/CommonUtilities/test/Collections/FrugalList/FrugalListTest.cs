using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace AnakinRaW.CommonUtilities.Test.Collections.FrugalList;

public class FrugalListTest_String : FrugalListTestBase<string>
{
    protected override string CreateT(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes = new byte[stringLength];
        rand.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class List_Generic_Tests_string_ReadOnly : FrugalListTest_String
{
    protected override bool IsReadOnly => true;

    protected override IList<string> GenericIListFactory(int setLength)
    {
        return GenericFrugalListFactory(setLength).AsReadOnly();
    }

    protected override IList<string> GenericIListFactory()
    {
        return GenericFrugalListFactory().AsReadOnly();
    }

    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        return new List<ModifyEnumerable>();
    }
}

public class FrugalListTest_Int : FrugalListTestBase<int>
{
    protected override int CreateT(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }
}

public class List_Generic_Tests_int_ReadOnly : FrugalListTest_Int
{
    protected override bool IsReadOnly => true;
  
    protected override IList<int> GenericIListFactory(int setLength)
    {
        return GenericFrugalListFactory(setLength).AsReadOnly();
    }

    protected override IList<int> GenericIListFactory()
    {
        return GenericFrugalListFactory().AsReadOnly();
    }

    protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
    {
        return new List<ModifyEnumerable>();
    }
}

