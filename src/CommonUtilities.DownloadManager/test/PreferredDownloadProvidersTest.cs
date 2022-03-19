using System.Collections.Generic;
using System.Linq;
using Moq;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test;

public class PreferredDownloadProvidersTest
{
    private readonly PreferredDownloadProviders _provider;

    public PreferredDownloadProvidersTest()
    {
        _provider = new PreferredDownloadProviders();
    }

    [Fact]
    public void TestAddProviders()
    {
        _provider.LastSuccessfulProviderName = "1";
        _provider.LastSuccessfulProviderName = "2";
        _provider.LastSuccessfulProviderName = "3";

        var last = _provider.LastSuccessfulProviderName;
        Assert.Equal("3", last);
    }

    [Fact]
    public void TestGetPriorityEmpty()
    {
        _provider.LastSuccessfulProviderName = "A";
        _provider.LastSuccessfulProviderName = "B";
        _provider.LastSuccessfulProviderName = "A";

        var last = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider>());
        Assert.Empty(last);
    }

    [Fact]
    public void TestGetWithHighestPriority()
    {
        _provider.LastSuccessfulProviderName = "A";
        _provider.LastSuccessfulProviderName = "A";
        _provider.LastSuccessfulProviderName = "B";
        
        var a = new Mock<IDownloadProvider>();
        a.Setup(p => p.Name).Returns("A");

        var b = new Mock<IDownloadProvider>();
        b.Setup(p => p.Name).Returns("B");

        var providers = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider> { b.Object, a.Object }).Select(x => x.Name).ToList();
        Assert.Equal(new List<string> { "A", "B" }, providers);
    }

    [Fact]
    public void TestGetSamePriority()
    {
        _provider.LastSuccessfulProviderName = "A";
        _provider.LastSuccessfulProviderName = "B";

        var a = new Mock<IDownloadProvider>();
        a.Setup(p => p.Name).Returns("A");

        var b = new Mock<IDownloadProvider>();
        b.Setup(p => p.Name).Returns("B");

        var providers = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider> { a.Object, b.Object }).Select(x => x.Name).ToList();
        Assert.Contains("A", providers);
        Assert.Contains("B", providers);
    }

    [Fact]
    public void TestGetPriorityNotExisting()
    {
        _provider.LastSuccessfulProviderName = "A";
        _provider.LastSuccessfulProviderName = "B";

        var c = new Mock<IDownloadProvider>();
        c.Setup(p => p.Name).Returns("C");
        

        var providers = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider>{c.Object}).Select(x => x.Name).ToList();
        Assert.Equal(new List<string> {"C"}, providers);
    }
}