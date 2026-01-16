using System;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.IList;

// ReSharper disable once InconsistentNaming
public class ValueListDictionary_Values : ValueListDictionary_Values_TestSuite
{
    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory()
    {
        return (ValueListDictionary<string, string>)MutableValueListDictionaryFactory();
    }

    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory(IValueListDictionary<string, string> dictionary)
    {
        return (ValueListDictionary<string, string>)dictionary;
    }

    [Fact]
    public void ValueListDictionary_ValueCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new ValueListDictionary<string, string>.ValueCollection(null!));
    }
}