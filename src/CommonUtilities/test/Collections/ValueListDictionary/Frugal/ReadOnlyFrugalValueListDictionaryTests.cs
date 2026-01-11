using System;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.Frugal;

public class ReadOnlyFrugalValueListDictionaryTest_string_string
    : ReadOnlyFrugalValueListDictionaryTestBase<string, string>
{
    protected override bool DefaultValueAllowed => false;

    protected override string CreateTKey(int seed)
    {
        var stringLength = seed % 10 + 5;
        var rand = new Random(seed);
        var bytes1 = new byte[stringLength];
        rand.NextBytes(bytes1);
        return Convert.ToBase64String(bytes1);
    }

    protected override string CreateTValue(int seed)
    {
        return CreateTKey(seed);
    }
}

public class ReadOnlyFrugalValueListDictionaryTest_int_int : ReadOnlyFrugalValueListDictionaryTestBase<int, int>
{
    protected override bool DefaultValueAllowed => true;

    protected override int CreateTKey(int seed)
    {
        var rand = new Random(seed);
        return rand.Next();
    }

    protected override int CreateTValue(int seed)
    {
        return CreateTKey(seed);
    }
}