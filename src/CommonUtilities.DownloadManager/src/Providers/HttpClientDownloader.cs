﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

internal class HttpClientDownloader : DownloadProviderBase
{
    private readonly ILogger? _logger;

    static HttpClientDownloader()
    {
        if (ServicePointManager.SecurityProtocol == 0)
            return;
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    public HttpClientDownloader(IServiceProvider services) : base("HttpClient", DownloadKind.Internet)
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
        var response = await GetHttpResponse(uri, summary, webRequest, cancellationToken).ConfigureAwait(false);
        try
        {
            if (response is not null)
            {
                if (response.IsSuccessStatusCode)
                {
#if NET
                    await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                    using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                    var contentLengthData = response.Content.Headers.ContentLength ?? 0;
                    var contentLength = contentLengthData;

                    var requestRegistration = cancellationToken.Register(webRequest.Dispose);
                    try
                    {
                        summary.DownloadedSize = await StreamUtilities.CopyStreamWithProgressAsync(responseStream, contentLength, outputStream, progress,
                            cancellationToken).ConfigureAwait(false);
                        return summary;
                    }
                    finally
                    {
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
                        await requestRegistration.DisposeAsync();
#else
                        requestRegistration.Dispose();
#endif

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

    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        return request;
    }

    private async Task<HttpResponseMessage?> GetHttpResponse(Uri uri, DownloadResult result, HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        var success = false;
        try
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(120000)
            };

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            
            var responseUri = response.RequestMessage?.RequestUri?.ToString();
            if (!string.IsNullOrEmpty(responseUri) &&
                !uri.ToString().Equals(responseUri, StringComparison.InvariantCultureIgnoreCase))
            {
                result.Uri = responseUri!;
                _logger?.LogTrace($"Uri '{uri}' redirected to '{responseUri}'");
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    success = true;
                    return response;
                default:
                    _logger?.LogWarning($"Error getting response. Status Code: {response.StatusCode}");
                    break;
            }
        }
        catch (HttpRequestException)
        {
            var errorMessage = cancellationToken.IsCancellationRequested
                ? "GetHttpResponse failed along with a cancellation request"
                : "GetHttpResponse failed";
            if (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogTrace(
                    "HttpClient error with '" + uri.AbsoluteUri + "' - " + errorMessage);
                cancellationToken.ThrowIfCancellationRequested();
            }
            _logger?.LogTrace("WebClient error - '" + uri.AbsoluteUri + "'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "General exception error in HttpClient");
            throw;
        }
        finally
        {
            if (response != null & !success)
                response!.Dispose();
        }
        return null;
    }
}