using System;
using System.Collections;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class ThrowHelperTest
{
    [Fact]
    public static void ThrowIfNullOrEmpty_ThrowsForInvalidInput()
    {
        AssertExtensions.Throws<ArgumentNullException>(null, () => ThrowHelper.ThrowIfNullOrEmpty(null, null));
        AssertExtensions.Throws<ArgumentNullException>("something", () => ThrowHelper.ThrowIfNullOrEmpty(null, "something"));

        AssertExtensions.Throws<ArgumentException>(null, () => ThrowHelper.ThrowIfNullOrEmpty("", null));
        AssertExtensions.Throws<ArgumentException>("something", () => ThrowHelper.ThrowIfNullOrEmpty("", "something"));

        ThrowHelper.ThrowIfNullOrEmpty(" ");
        ThrowHelper.ThrowIfNullOrEmpty(" ", "something");
        ThrowHelper.ThrowIfNullOrEmpty("abc", "something");
    }

    [Fact]
    public static void ThrowIfNullOrEmpty_UsesArgumentExpression_ParameterNameMatches()
    {
        string someString = null;
        AssertExtensions.Throws<ArgumentNullException>(nameof(someString), () => ThrowHelper.ThrowIfNullOrEmpty(someString));

        someString = "";
        AssertExtensions.Throws<ArgumentException>(nameof(someString), () => ThrowHelper.ThrowIfNullOrEmpty(someString));

        someString = "abc";
        ThrowHelper.ThrowIfNullOrEmpty(someString);
    }


    [Fact]
    public static void ThrowIfCollectionNullOrEmpty_ThrowsForInvalidInput()
    {
        AssertExtensions.Throws<ArgumentNullException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty(null, null));
        AssertExtensions.Throws<ArgumentNullException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>)null, null));
        AssertExtensions.Throws<ArgumentNullException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)null, null));
        AssertExtensions.Throws<ArgumentNullException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty(null, "something"));
        AssertExtensions.Throws<ArgumentNullException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>)null, "something"));
        AssertExtensions.Throws<ArgumentNullException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)null, "something"));

        AssertExtensions.Throws<ArgumentException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<int>(), null));
        AssertExtensions.Throws<ArgumentException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>)new List<int>(), null));
        AssertExtensions.Throws<ArgumentException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<string>(), null));
        AssertExtensions.Throws<ArgumentException>(null, () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)new List<string>(), null));

        AssertExtensions.Throws<ArgumentException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList) new List<int>(), "something"));
        AssertExtensions.Throws<ArgumentException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>) new List<int>(), "something"));

        AssertExtensions.Throws<ArgumentException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<string>(), "something"));
        AssertExtensions.Throws<ArgumentException>("something", () => ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)new List<string>(), "something"));

        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<int>{1});
        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>)new List<int>{1});

        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<string> { "A" });
        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)new List<string> { "A" });


        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<int> { 1 }, "something");
        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<int>)new List<int> { 1 }, "something");

        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList)new List<string> { "A" }, "something");
        ThrowHelper.ThrowIfCollectionNullOrEmpty((IList<string>)new List<string> { "A" }, "something");
    }

}