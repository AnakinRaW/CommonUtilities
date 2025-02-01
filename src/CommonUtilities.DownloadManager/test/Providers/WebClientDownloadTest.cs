#if NETFRAMEWORK
using System;
using System.Net;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class WebClientDownloadTest : InternetDownloadTest
{
    protected override Type ExpectedSourceNotFoundExceptionType => typeof(WebException);

    protected override IDownloadProvider CreateProvider()
    {
        return new WebClientDownloader(ServiceProvider);
    }
}
#endif
