using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Collections;

namespace AnakinRaW.CommonUtilities.Test.Collections;

public class ReadOnlyFrugalListTest_String : ReadOnlyFrugalListTestBase<string>
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

public class ReadOnlyFrugalListTest_Int : ReadOnlyFrugalListTestBase<int>
{
    protected override int CreateT(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }
}


public class ReadOnlyFrugalListTest_Int_FromFrugal : ReadOnlyFrugalListTestBase<int>
{
    protected override int CreateT(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }

    protected override ReadOnlyFrugalList<int> GenericReadOnlyListFrugalListFactory(IEnumerable<int> enumerable)
    {
        var frugal = new FrugalList<int>(enumerable);
        return frugal.AsReadOnly();
    }
}