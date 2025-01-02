using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Validation;

public class AlwaysValidDownloadValidatorTest
{
    [Fact]
    public async Task Test_Validate_IsValid()
    {
        var validator = AlwaysValidDownloadValidator.Instance;
        var result = await validator.Validate(null!, -1);
        Assert.True(result);
    }
}