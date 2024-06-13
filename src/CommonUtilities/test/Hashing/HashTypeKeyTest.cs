using System;
using AnakinRaW.CommonUtilities.Hashing;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Hashing;

public class HashTypeKeyTest
{
    [Fact]
    public void Test_Ctor()
    {
        var key = new HashTypeKey("123", 1);
        Assert.Equal("123", key.Name);
        Assert.Equal(1, key.HashSize);
    }

    [Fact]
    public void Test_None()
    {
        var key = HashTypeKey.None;
        Assert.Null(key.Name);
        Assert.Equal(0, key.GetHashCode());
        Assert.Equal(0, key.HashSize);
    }

    [Fact]
    public void Test_Ctor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HashTypeKey(null!, 1));
        Assert.Throws<ArgumentException>(() => new HashTypeKey("", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashTypeKey("123", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashTypeKey("123", -1));
    }

    [Fact]
    public void Test_Equals_GetHashCode()
    {
        var key1 = new HashTypeKey("abc", 1);
        var key2 = new HashTypeKey("ABC", 1);
        var key3 = new HashTypeKey("abc", 2);
        var key4 = new HashTypeKey("def", 2);

        Assert.True(key1 == key2);
        Assert.True(key1 == key3);
        Assert.True(key1 != key4);

        Assert.True(key1.Equals(key2));
        Assert.True(key1.Equals(key3));
        Assert.True(!key1.Equals(key4));

        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
        Assert.Equal(key1.GetHashCode(), key3.GetHashCode());
        Assert.NotEqual(key1.GetHashCode(), key4.GetHashCode());
    }
}