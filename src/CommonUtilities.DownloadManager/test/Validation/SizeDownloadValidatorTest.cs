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
        var result = await validator.ValidateAsync(null!, 123, TestContext.Current.CancellationToken);
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(321)]
    public async Task Validate_IsInvalid(int actualValue)
    {
        var validator = new SizeDownloadValidator(123);
        var result = await validator.ValidateAsync(null!, actualValue, TestContext.Current.CancellationToken);
        Assert.False(result);
    }
}