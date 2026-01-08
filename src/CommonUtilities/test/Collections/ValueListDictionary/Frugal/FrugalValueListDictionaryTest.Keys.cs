using System;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.Frugal;

// ReSharper disable once InconsistentNaming
public class FrugalValueListDictionary_Keys : ValueListDictionary_Keys_TestSuite
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
    public void ValueListDictionary_KeyCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new FrugalValueListDictionary<string, string>.KeyCollection(null!));
    }
}