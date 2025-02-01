using System.Threading;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Validation;

public class AlwaysValidDownloadValidatorTest
{
    [Fact]
    public async Task Validate_IsValid_NullStream_CancelledToken_NegativeBytes()
    {
        var validator = AlwaysValidDownloadValidator.Instance;
        var result = await validator.Validate(null!, -1, new CancellationToken(true));
        Assert.True(result);
    }
}