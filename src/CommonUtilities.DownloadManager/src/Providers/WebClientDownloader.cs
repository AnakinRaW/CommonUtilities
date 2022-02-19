#if !NET6_0_OR_GREATER
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Providers;


internal class WebClientDownloader : DownloadProviderBase
{
    private readonly ILogger? _logger;

    static WebClientDownloader()
    {
        if (ServicePointManager.SecurityProtocol == 0)
            return;
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    public WebClientDownloader(IServiceProvider services) : base("WebClient", new[] { DownloadSource.Internet })
    {
        Requires.NotNull(services, nameof(services));
        _logger = services.GetService<ILogger>();
    }

    protected override DownloadSummary DownloadCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadSummary();

        var webResponse = GetWebResponse(uri, ref summary, out var webRequest, cancellationToken);
        try
        {
            if (webResponse != null)
            {
                var responseRegistration = cancellationToken.Register(() => webResponse.Close());
                try
                {
                    using var responseStream = webResponse.GetResponseStream();
                    var contentLength = webResponse.Headers["Content-Length"];
                    if (string.IsNullOrEmpty(contentLength))
                        throw new IOException("Error: Content-Length is missing from response header.");
                    var totalStreamLength = Convert.ToInt64(contentLength);
                    if (totalStreamLength.Equals(0L))
                        throw new IOException("Error: Response stream length is 0.");

                    var requestRegistration = cancellationToken.Register(() => webRequest!.Abort());
                    try
                    {
                        summary.DownloadedSize = StreamUtilities.CopyStreamWithProgress(responseStream,
                            totalStreamLength, outputStream, progress,
                            cancellationToken);
                        return summary;
                    }
                    finally
                    {
                        requestRegistration.Dispose();
                    }
                }
                catch (WebException ex)
                {
                    var message = cancellationToken.IsCancellationRequested
                        ? "DownloadCore failed along with a cancellation request."
                        : "DownloadCore failed";
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger?.LogTrace("WebClient error '" + ex.Status + "' with '" + uri.AbsoluteUri + "' - " +
                                          message);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        _logger?.LogTrace("WebClient error '" + ex.Status + "' with '" + uri.AbsoluteUri + "'.");
                        throw;
                    }
                }
                finally
                {
                    responseRegistration.Dispose();
                }
            }

            return summary;
        }
        finally
        {
            webResponse?.Dispose();
        }
    }

    private HttpWebResponse? GetWebResponse(Uri uri, ref DownloadSummary summary, out HttpWebRequest? webRequest,
        CancellationToken cancellationToken)
    {
        HttpWebResponse? httpWebResponse = null;
        var successful = true;
        try
        {
            webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            webRequest.KeepAlive = true;
            webRequest.Timeout = 120000;

            var requestCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            webRequest.CachePolicy = requestCachePolicy;
            
            var registerWebRequest = webRequest;
            using (cancellationToken.Register(() => registerWebRequest.Abort()))
                httpWebResponse = (HttpWebResponse)webRequest.GetResponse();

            var responseUri = httpWebResponse.ResponseUri.ToString();
            if (!string.IsNullOrEmpty(responseUri) &&
                !uri.ToString().EndsWith(responseUri, StringComparison.InvariantCultureIgnoreCase))
            {
                summary.FinalUri = responseUri;
                _logger?.LogTrace($"Uri '{uri}' + redirected to '{responseUri}'");
            }

            switch (httpWebResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    successful = false;
                    return httpWebResponse;
            }
        }
        catch (WebException ex)
        {
            var errorMessage = cancellationToken.IsCancellationRequested
                ? "GetWebResponse failed along with a cancellation request"
                : "GetWebResponse failed";
            if (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogTrace(
                    "WebClient error '" + ex.Status + "' with '" + uri.AbsoluteUri + "' - " + errorMessage);
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger?.LogTrace("WebClient error '" + ex.Status + "' - '" + uri.AbsoluteUri + "'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "General exception error in web client.");
            throw;
        }
        finally
        {
            if (httpWebResponse != null && successful)
                httpWebResponse.Close();
        }

        webRequest = null;
        return null;
    }

}
#endif