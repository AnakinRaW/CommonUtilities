using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class DownloadOptionsTest
{
    [Theory]
    [InlineData("user", "token")]
    [InlineData(null, null)]
    public void Ctor(string? userAgent, string? authToken)
    {
        var o = new DownloadOptions
        {
            UserAgent = userAgent,
            AuthenticationToken = authToken
        };

        Assert.Equal(userAgent, o.UserAgent);
        Assert.Equal(authToken, o.AuthenticationToken);
    }
}