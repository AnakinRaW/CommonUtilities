using System;
using AnakinRaW.CommonUtilities.Collections;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections.ValueListDictionary.IList;

// ReSharper disable once InconsistentNaming
public class ReadOnlyValueListDictionary_Keys : ValueListDictionary_Keys_TestSuite
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
    public void ReadOnlyValueListDictionary_KeyCollection_Constructor_NullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyValueListDictionary<string, string>.KeyCollection(null!));
    }
}