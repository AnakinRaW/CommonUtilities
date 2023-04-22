using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Configuration;

public class DownloadManagerConfigurationProviderBaseTests
{
    [Fact]
    public void GetConfiguration_CreatesNewConfiguration_Once()
    {
        var m = new Mock<DownloadManagerConfigurationProviderBase>();

        m.Protected().Setup<IDownloadManagerConfiguration>("CreateConfiguration")
            .Returns(new Mock<IDownloadManagerConfiguration>().Object);

        var configuration1 = m.Object.GetConfiguration();
        var configuration2 = m.Object.GetConfiguration();

        Assert.Same(configuration1, configuration2);

        m.Protected().Verify("CreateConfiguration", Times.Once(), false);
    }
}