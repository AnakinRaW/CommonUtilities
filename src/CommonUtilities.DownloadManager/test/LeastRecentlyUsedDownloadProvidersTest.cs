using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Moq;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class LeastRecentlyUsedDownloadProvidersTest
{
    private readonly LeastRecentlyUsedDownloadProviders _provider = new();

    [Fact]
    public void TestAddProviders()
    {
        _provider.LastSuccessfulProvider = "1";
        _provider.LastSuccessfulProvider = "2";
        _provider.LastSuccessfulProvider = "3";

        var last = _provider.LastSuccessfulProvider;
        Assert.Equal("3", last);
    }

    [Fact]
    public void TestGetPriorityEmpty()
    {
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";
        _provider.LastSuccessfulProvider = "A";

        var last = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider>());
        Assert.Empty(last);
    }

    [Fact]
    public void TestGetWithHighestPriority()
    {
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";
        
        var a = new Mock<IDownloadProvider>();
        a.Setup(p => p.Name).Returns("A");

        var b = new Mock<IDownloadProvider>();
        b.Setup(p => p.Name).Returns("B");

        var providers = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider> { b.Object, a.Object }).Select(x => x.Name).ToList();
        Assert.Equal(["A", "B"], providers);
    }

    [Fact]
    public void TestGetSamePriority()
    {
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";

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
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";

        var c = new Mock<IDownloadProvider>();
        c.Setup(p => p.Name).Returns("C");
        

        var providers = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider>{c.Object}).Select(x => x.Name).ToList();
        Assert.Equal(["C"], providers);
    }
}