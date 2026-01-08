using AnakinRaW.CommonUtilities.Collections;
using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.Frugal;

public class FrugalValueListDictionary_Values : ValueListDictionary_Values_TestSuite
{
    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory()
    {
        return (FrugalValueListDictionary<string, string>)MutableValueListDictionaryFactory();
    }

    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory(IValueListDictionary<string, string> dictionary)
    {
        return (FrugalValueListDictionary<string, string>)dictionary;
    }

    protected override IValueListDictionary<string, string> MutableValueListDictionaryFactory()
    {
        return new FrugalValueListDictionary<string, string>();
    }

    [Fact]
    public void ValueListDictionary_ValueCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new FrugalValueListDictionary<string, string>.ValueCollection(null!));
    }
}
