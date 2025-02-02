using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using System.Net.Http;
using System;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public class HttpClientDownloadTest : InternetDownloadTest
{
    protected override Type ExpectedSourceNotFoundExceptionType => typeof(HttpRequestException);

    protected override IDownloadProvider CreateProvider()
    {
        return new HttpClientDownloader(ServiceProvider);
    }
}