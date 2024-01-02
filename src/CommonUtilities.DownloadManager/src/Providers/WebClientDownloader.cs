#if !NET
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

internal class WebClientDownloader : DownloadProviderBase
{
    private readonly ILogger? _logger;

    static WebClientDownloader()
    {
        if (ServicePointManager.SecurityProtocol == 0)
            return;
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    public WebClientDownloader(IServiceProvider services) : base("WebClient", DownloadSource.Internet)
    {
        Requires.NotNull(services, nameof(services));
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override async Task<DownloadSummary> DownloadAsyncCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadSummary();

        var webRequest = CreateRequest(uri);

        var webResponse = await GetWebResponse(uri, summary, webRequest, cancellationToken).ConfigureAwait(false);
        try
        {
            if (webResponse != null)
            {
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
                        summary.DownloadedSize = await StreamUtilities.CopyStreamWithProgressAsync(responseStream,
                            totalStreamLength, outputStream, progress,
                            cancellationToken).ConfigureAwait(false);
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
            }

            return summary;
        }
        finally
        {
            webResponse?.Dispose();
        }
    }

    private static HttpWebRequest CreateRequest(Uri uri)
    {
        var webRequest = (HttpWebRequest)WebRequest.Create(uri);
        webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
        webRequest.KeepAlive = true;
        webRequest.Timeout = 120000;

        var requestCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
        webRequest.CachePolicy = requestCachePolicy;
        return webRequest;
    }

    private async Task<HttpWebResponse?> GetWebResponse(Uri uri, DownloadSummary summary, HttpWebRequest webRequest,
        CancellationToken cancellationToken)
    {
        HttpWebResponse? httpWebResponse = null;
        var successful = true;
        try
        {
            using (cancellationToken.Register(webRequest.Abort))
                httpWebResponse = (HttpWebResponse)await webRequest.GetResponseAsync().ConfigureAwait(false);

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
        return null;
    }
}
#endif