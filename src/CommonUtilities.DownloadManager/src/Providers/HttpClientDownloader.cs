using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.DownloadManager.Providers;

/// <summary>
/// A download provider using .NET's HttpClient implementation to download files from the Internet.
/// </summary>
public sealed class HttpClientDownloader : DownloadProviderBase
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientDownloader"/> class.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public HttpClientDownloader(IServiceProvider services) 
        : base("HttpClient", DownloadKind.Internet, services)
    {
        _logger = ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc />
    protected override async Task<DownloadResult> DownloadAsyncCore(
        Uri uri, 
        Stream outputStream, 
        DownloadUpdateCallback? progress,
        DownloadOptions? downloadOptions,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadResult(uri);
        var webRequest = CreateRequest(uri);
        var response = await GetHttpResponse(uri, downloadOptions, summary, webRequest, cancellationToken).ConfigureAwait(false);
        try
        {
            if (response is not null)
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

    private async Task<HttpResponseMessage?> GetHttpResponse(
        Uri uri,
        DownloadOptions? downloadOptions,
        DownloadResult result, 
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        var success = false;
        try
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(120000),
            };

            if (downloadOptions is not null)
            {
                if (!string.IsNullOrEmpty(downloadOptions.UserAgent))
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd(downloadOptions.UserAgent);

                if (!string.IsNullOrWhiteSpace(downloadOptions.AuthenticationToken))
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", downloadOptions.AuthenticationToken);
            }

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var redirectUri = response.Headers.Location;
            if (redirectUri is not null)
            {
                _logger?.LogTrace($"Uri '{uri}' redirected to '{redirectUri}'");
                result.Uri = redirectUri;
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