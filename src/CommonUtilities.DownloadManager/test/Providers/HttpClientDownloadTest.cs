using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using System.Net.Http;
using System;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class HttpClientDownloadTest : InternetDownloadTest
{
    protected override Type ExpectedSourceNotFoundExceptionType => typeof(HttpRequestException);

    protected override IDownloadProvider CreateProvider()
    {
        return new HttpClientDownloader(ServiceProvider);
    }

    protected override void AssertRequiredUserAgentMissingException(Exception exception)
    { 
        Assert.IsType<HttpRequestException>(exception);
    }
}