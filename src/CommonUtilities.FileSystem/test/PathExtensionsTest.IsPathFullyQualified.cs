using System;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Testing;
using Testably.Abstractions.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test;

public class IsPathFullyQualifiedTest
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    [Fact]
    public void IsPathFullyQualified_NullArgument()
    {
        Assert.Throws<ArgumentNullException>(() => _fileSystem.Path.IsPathFullyQualified(((string?)null)!));
    }

    [Fact]
    public void IsPathFullyQualified_Empty()
    {
        Assert.False(_fileSystem.Path.IsPathFullyQualified(""));
        Assert.False(_fileSystem.Path.IsPathFullyQualified(ReadOnlySpan<char>.Empty));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData("/")]
    [InlineData(@"\")]
    [InlineData(".")]
    [InlineData("C:")]
    [InlineData("C:foo.txt")]
    public void IsPathFullyQualified_Windows_Invalid(string path)
    {
        Assert.False(_fileSystem.Path.IsPathFullyQualified(path));
        Assert.False(_fileSystem.Path.IsPathFullyQualified(path.AsSpan()));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Windows)]
    [InlineData(@"\\")]
    [InlineData(@"\\\")]
    [InlineData(@"\\Server")]
    [InlineData(@"\\Server\Foo.txt")]
    [InlineData(@"\\Server\Share\Foo.txt")]
    [InlineData(@"\\Server\Share\Test\Foo.txt")]
    [InlineData(@"C:\")]
    [InlineData(@"C:\foo1")]
    [InlineData(@"C:\\")]
    [InlineData(@"C:\\foo2")]
    [InlineData(@"C:/")]
    [InlineData(@"C:/foo1")]
    [InlineData(@"C://")]
    [InlineData(@"C://foo2")]
    public void IsPathFullyQualified_Windows_Valid(string path)
    {
        Assert.True(_fileSystem.Path.IsPathFullyQualified(path));
        Assert.True(_fileSystem.Path.IsPathFullyQualified(path.AsSpan()));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData(@"\")]
    [InlineData(@"\\")]
    [InlineData(".")]
    [InlineData("./foo.txt")]
    [InlineData("..")]
    [InlineData("../foo.txt")]
    [InlineData(@"C:")]
    [InlineData(@"C:/")]
    [InlineData(@"C://")]
    public void IsPathFullyQualified_Unix_Invalid(string path)
    {
        Assert.False(_fileSystem.Path.IsPathFullyQualified(path));
        Assert.False(_fileSystem.Path.IsPathFullyQualified(path.AsSpan()));
    }

    [PlatformSpecificTheory(TestPlatformIdentifier.Linux)]
    [InlineData("/")]
    [InlineData("/foo.txt")]
    [InlineData("/..")]
    [InlineData("//")]
    [InlineData("//foo.txt")]
    [InlineData("//..")]
    public void IsPathFullyQualified_Unix_Valid(string path)
    {
        Assert.True(_fileSystem.Path.IsPathFullyQualified(path));
        Assert.True(_fileSystem.Path.IsPathFullyQualified(path.AsSpan()));
    }

}