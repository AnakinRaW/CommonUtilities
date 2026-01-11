using System;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.Frugal;

// ReSharper disable once InconsistentNaming

public class ReadOnlyFrugalValueListDictionary_Values : ValueListDictionary_Values_TestSuite
{
    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory()
    {
        return new ReadOnlyFrugalValueListDictionary<string, string>(new FrugalValueListDictionary<string, string>());
    }

    protected override IValueListDictionary<string, string> MutableValueListDictionaryFactory()
    {
        return new FrugalValueListDictionary<string, string>();
    }

    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory(IValueListDictionary<string, string> dictionary)
    {
        return new ReadOnlyFrugalValueListDictionary<string, string>((IReadOnlyFrugalValueListDictionary<string, string>)dictionary);
    }

    [Fact]
    public void ReadOnlyFrugalValueListDictionary_ValueCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyFrugalValueListDictionary<string, string>.ValueCollection(null!));
    }
}

