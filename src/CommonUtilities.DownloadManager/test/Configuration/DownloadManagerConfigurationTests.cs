using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Configuration;

public class DownloadManagerConfigurationTests
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var defaultConfig = DownloadManagerConfiguration.Default;

        Assert.Equal(5000, defaultConfig.DownloadRetryDelay);
        Assert.False(defaultConfig.AllowEmptyFileDownload);
        Assert.Equal(ValidationPolicy.NoValidation, defaultConfig.ValidationPolicy);
        Assert.Equal(InternetClient.HttpClient, defaultConfig.InternetClient);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var config = new DownloadManagerConfiguration
        {
            AllowEmptyFileDownload = true,
            ValidationPolicy = ValidationPolicy.Required,
            InternetClient = InternetClient.HttpClient
        };

        Assert.True(config.AllowEmptyFileDownload);
        Assert.Equal(ValidationPolicy.Required, config.ValidationPolicy);
        Assert.Equal(InternetClient.HttpClient, config.InternetClient);
    }

    [Fact]
    public void DownloadRetryDelay_ShouldBeSettable()
    {
        var config = new DownloadManagerConfiguration
        {
            DownloadRetryDelay = 10000
        };

        Assert.Equal(10000, config.DownloadRetryDelay);
    }
}