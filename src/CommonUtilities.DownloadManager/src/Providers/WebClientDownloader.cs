#if !NET
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public WebClientDownloader(IServiceProvider services) : base("WebClient", DownloadKind.Internet)
    {
        if (services == null) 
            throw new ArgumentNullException(nameof(services));
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override async Task<DownloadResult> DownloadAsyncCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadResult();

        var webRequest = CreateRequest(uri);

        var webResponse = await GetWebResponse(uri, summary, webRequest, cancellationToken).ConfigureAwait(false);
        try
        {
            if (webResponse != null)
            {
                try
                {
#if NET || NETSTANDARD2_1
                    await
#endif
                    using var responseStream = webResponse.GetResponseStream();
                    var contentLength = webResponse.Headers["Content-Length"];
                    if (string.IsNullOrEmpty(contentLength))
                        throw new IOException("Error: Content-Length is missing from response header.");
                    var totalStreamLength = Convert.ToInt64(contentLength);
                    if (totalStreamLength.Equals(0L))
                        throw new IOException("Error: Response stream length is 0.");

                    var requestRegistration = cancellationToken.Register(webRequest.Abort);
                    try
                    {
                        summary.DownloadedSize = await StreamUtilities.CopyStreamWithProgressAsync(responseStream,
                            totalStreamLength, outputStream, progress,
                            cancellationToken).ConfigureAwait(false);
                        return summary;
                    }
                    finally
                    {
#if NET || NETSTANDARD2_1
                        await requestRegistration.DisposeAsync();
#else
                        requestRegistration.Dispose();
#endif
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

    private async Task<HttpWebResponse?> GetWebResponse(Uri uri, DownloadResult result, HttpWebRequest webRequest,
        CancellationToken cancellationToken)
    {
        HttpWebResponse? httpWebResponse = null;
        var success = false;
        try
        {
#if NET || NETSTANDARD2_1
            await
#endif
            using (cancellationToken.Register(webRequest.Abort))
                httpWebResponse = (HttpWebResponse)await webRequest.GetResponseAsync().ConfigureAwait(false);

            var responseUri = httpWebResponse.ResponseUri.ToString();
            if (!string.IsNullOrEmpty(responseUri) &&
                !uri.ToString().EndsWith(responseUri, StringComparison.InvariantCultureIgnoreCase))
            {
                result.Uri = responseUri;
                _logger?.LogTrace($"Uri '{uri}' + redirected to '{responseUri}'");
            }

            switch (httpWebResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    success = true;
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
            if (httpWebResponse != null & !success)
                httpWebResponse!.Close();
        }
        return null;
    }
}
#endif