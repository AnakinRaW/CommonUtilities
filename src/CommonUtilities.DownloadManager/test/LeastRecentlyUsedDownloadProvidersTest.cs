using System.Collections.Generic;
using System.Linq;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class LeastRecentlyUsedDownloadProvidersTest : CommonTestBase
{
    private readonly LeastRecentlyUsedDownloadProviders _provider = new();

    [Fact]
    public void LastSuccessfulProvider()
    {
        _provider.LastSuccessfulProvider = "1";
        _provider.LastSuccessfulProvider = "2";
        _provider.LastSuccessfulProvider = "3";

        var last = _provider.LastSuccessfulProvider;
        Assert.Equal("3", last);
    }

    [Fact]
    public void GetProvidersInPriorityOrder_EmptyProvided()
    {
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";
        _provider.LastSuccessfulProvider = "A";

        var last = _provider.GetProvidersInPriorityOrder(new List<IDownloadProvider>());
        Assert.Empty(last);
    }

    [Fact]
    public void GetProvidersInPriorityOrder_GetWithHighestPriority()
    {
        _provider.LastSuccessfulProvider = "File";
        _provider.LastSuccessfulProvider = "File";
        _provider.LastSuccessfulProvider = "HttpClient";
        
        var a = new FileDownloader(ServiceProvider);
        var b = new HttpClientDownloader(ServiceProvider);

        var providers = _provider.GetProvidersInPriorityOrder([b, a])
            .Select(x => x.Name)
            .ToList();
        Assert.Equal(["File", "HttpClient"], providers);
    }

    [Fact]
    public void GetProvidersInPriorityOrder_TestGetSamePriority()
    {
        _provider.LastSuccessfulProvider = "File";
        _provider.LastSuccessfulProvider = "HttpClient";

        var a = new FileDownloader(ServiceProvider);
        var b = new HttpClientDownloader(ServiceProvider);

        var providers = _provider.GetProvidersInPriorityOrder([a, b])
            .Select(x => x.Name)
            .ToList();

        Assert.Contains("File", providers);
        Assert.Contains("HttpClient", providers);
    }

    [Fact]
    public void TestGetPriorityNotExisting()
    {
        _provider.LastSuccessfulProvider = "A";
        _provider.LastSuccessfulProvider = "B";

        var file = new FileDownloader(ServiceProvider);


        var providers = _provider.GetProvidersInPriorityOrder([file])
            .Select(x => x.Name)
            .ToList();
        Assert.Equal(["File"], providers);
    }
}