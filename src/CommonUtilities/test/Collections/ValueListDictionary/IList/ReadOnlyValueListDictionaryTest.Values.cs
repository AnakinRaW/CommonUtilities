using System;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.IList;

// ReSharper disable once InconsistentNaming

public class ReadOnlyValueListDictionary_Values : ValueListDictionary_Values_TestSuite
{
    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory()
    {
        return new ReadOnlyValueListDictionary<string, string>(new ValueListDictionary<string, string>());
    }

    protected override IReadOnlyValueListDictionary<string, string> ValueListDictionaryFactory(IValueListDictionary<string, string> dictionary)
    {
        return new ReadOnlyValueListDictionary<string, string>(dictionary);
    }

    [Fact]
    public void ReadOnlyValueListDictionary_ValueCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyValueListDictionary<string, string>.ValueCollection(null!));
    }
}

