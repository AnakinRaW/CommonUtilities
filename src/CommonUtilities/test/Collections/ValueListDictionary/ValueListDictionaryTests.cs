using System;
// ReSharper disable InconsistentNaming

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary;

public class ValueListDictionaryTest_string_string : ValueListDictionaryTestBase<string, string>
{
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

public class ValueListDictionaryTest_int_int : ValueListDictionaryTestBase<int, int>
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