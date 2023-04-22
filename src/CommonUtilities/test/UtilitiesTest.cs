using System.IO;
using System.IO.Abstractions.TestingHelpers;
using AnakinRaW.CommonUtilities.Verification;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class UtilitiesTest
{
    [Fact]
    public void TestGetStreamPath()
    {
        var fullPath = Path.GetFullPath("test.txt");
        Assert.Equal(fullPath, new FileStream("test.txt", FileMode.OpenOrCreate).Name);

        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("test.txt", new MockFileData(string.Empty));
        var file = fileSystem.FileInfo.New("test.txt");

        Assert.Equal(file.FullName, file.OpenRead().Name);

        Assert.Null(new MemoryStream().GetPathFromStream());
    }
}