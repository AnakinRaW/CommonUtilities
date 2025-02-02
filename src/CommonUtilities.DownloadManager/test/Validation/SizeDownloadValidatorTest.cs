using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Validation;

public class SizeDownloadValidatorTest
{
    [Fact]
    public async Task Validate_IsValid()
    {
        var validator = new SizeDownloadValidator(123);
        var result = await validator.Validate(null!, 123);
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(321)]
    public async Task Validate_IsInvalid(int actualValue)
    {
        var validator = new SizeDownloadValidator(123);
        var result = await validator.Validate(null!, actualValue);
        Assert.False(result);
    }
}