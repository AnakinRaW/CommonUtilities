#if NETFRAMEWORK
using System;
using System.Net;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class WebClientDownloadTest : InternetDownloadTest
{
    protected override Type ExpectedSourceNotFoundExceptionType => typeof(WebException);

    protected override IDownloadProvider CreateProvider()
    {
        return new WebClientDownloader(ServiceProvider);
    }

    protected override void AssertRequiredUserAgentMissingException(Exception exception)
    {
        var webException = Assert.IsType<WebException>(exception);
        Assert.Equal(HttpStatusCode.Forbidden, ((HttpWebResponse)webException.Response).StatusCode);
    }
}
#endif
