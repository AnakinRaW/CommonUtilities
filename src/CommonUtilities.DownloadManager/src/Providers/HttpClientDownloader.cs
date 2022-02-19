using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Providers;

internal class HttpClientDownloader : DownloadProviderBase
{
    private readonly ILogger? _logger;

    static HttpClientDownloader()
    {
        if (ServicePointManager.SecurityProtocol == 0)
            return;
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    public HttpClientDownloader(IServiceProvider services) : base("HttpClient", new[] { DownloadSource.Internet })
    {
        Requires.NotNull(services, nameof(services));
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override DownloadSummary DownloadCore(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadSummary();
        var response = GetWebResponse(uri, ref summary, out var webRequest, cancellationToken);
        try
        {
            if (response is not null)
            {
                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    var contentLengthData = response.Content.Headers.ContentLength;
                    if (contentLengthData is null or 0L)
                        throw new IOException("Error: Response stream length is 0.");

                    var contentLength = contentLengthData.Value;

                    StreamUtilities.CopyStreamWithProgress(responseStream, contentLength, outputStream, progress,
                        cancellationToken);

                    var requestRegistration = cancellationToken.Register(() => webRequest?.Dispose());
                    try
                    {
                        summary.DownloadedSize = StreamUtilities.CopyStreamWithProgress(responseStream, contentLength, outputStream, progress,
                            cancellationToken);
                        return summary;
                    }
                    finally
                    {
                        requestRegistration.Dispose();
                    }
                }
                else
                {
                    var message = cancellationToken.IsCancellationRequested
                        ? "DownloadCore failed along with a cancellation request."
                        : "DownloadCore failed";
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger?.LogTrace("WebClient error '" + response.StatusCode + "' with '" + uri.AbsoluteUri + "' - " +
                                          message);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        _logger?.LogTrace("WebClient error '" + response.StatusCode + "' with '" + uri.AbsoluteUri + "'.");
                        throw new HttpRequestException(message);
                    }
                }
            }
            return summary;
        }
        finally
        {
            response?.Dispose();
        }
    }

    private HttpResponseMessage? GetWebResponse(Uri uri, ref DownloadSummary summary, out HttpRequestMessage? request, CancellationToken cancellationToken)
    {
        var proxyResolution = ProxyResolution.Default;
        while (proxyResolution != ProxyResolution.Error)
        {
            HttpResponseMessage? response = null;
            var resolutionSuccess = false;
            try
            {

                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                var client = new HttpClient(handler)
                {
                    MaxResponseContentBufferSize = 0,
                    Timeout = TimeSpan.FromMilliseconds(120000)
                };
                request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("defalte"));
                switch (proxyResolution)
                {
                    case ProxyResolution.DefaultCredentialsOrNoAutoProxy:
                        handler.UseDefaultCredentials = true;
                        break;
                    case ProxyResolution.NetworkCredentials:
                        handler.UseDefaultCredentials = false;
                        handler.Proxy = WebRequest.GetSystemWebProxy();
                        handler.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                        break;
                    case ProxyResolution.DirectAccess:
                        handler.Proxy = null;
                        break;
                }

                response = client.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
                var responseUri = response.RequestMessage?.RequestUri?.ToString();
                if (!string.IsNullOrEmpty(responseUri) &&
                    !uri.ToString().Equals(responseUri, StringComparison.InvariantCultureIgnoreCase))
                {
                    summary!.FinalUri = responseUri!;
                    _logger?.LogTrace($"Uri '{uri}' redirected to '{responseUri}'");
                }

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        summary!.ProxyResolution = proxyResolution;
                        resolutionSuccess = true;
                        return response;
                    case HttpStatusCode.UseProxy:
                    case HttpStatusCode.ProxyAuthenticationRequired:
                    case HttpStatusCode.GatewayTimeout:
                        ++proxyResolution;
                        if (proxyResolution == ProxyResolution.Error)
                        {
                            _logger?.LogTrace($"WebResponse error '{response.StatusCode}' with '{uri}'.");
                            WrappedWebException.Throw((int)response.StatusCode, "HttpClient.Send",
                                summary.FinalUri);
                            continue;
                        }

                        _logger?.LogTrace(
                            $"WebResponse error '{response.StatusCode}' - '{uri.AbsoluteUri}'. Reattempt with proxy set to '{proxyResolution}'");
                        continue;
                    default:
                        proxyResolution = ProxyResolution.Error;
                        _logger?.LogTrace($"WebResponse error '{response.StatusCode}'  - '{uri.AbsoluteUri}'.");
                        WrappedWebException.Throw((int)response.StatusCode, "HttpClient.Send", summary.FinalUri);
                        continue;
                }
            }
            catch (WrappedWebException ex)
            {
                if (proxyResolution == ProxyResolution.Error)
                {
                    _logger?.LogTrace($"WebResponse exception '{ex.Status}' with '{uri}'.");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "General exception error in HttpClient");
                throw;
            }
            finally
            {
                if(response != null && !resolutionSuccess)
                    response.Dispose();
            }
        }

        request = null;
        return null;
    }
}