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

    public HttpClientDownloader(IServiceProvider services) : base("HttpClient", DownloadSource.Internet)
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

    private HttpResponseMessage? GetWebResponse(Uri uri, ref DownloadSummary summary, out HttpRequestMessage? request,
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
            request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("defalte"));
            response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();
            var responseUri = response.RequestMessage?.RequestUri?.ToString();
            if (!string.IsNullOrEmpty(responseUri) &&
                !uri.ToString().Equals(responseUri, StringComparison.InvariantCultureIgnoreCase))
            {
                summary.FinalUri = responseUri!;
                _logger?.LogTrace($"Uri '{uri}' redirected to '{responseUri}'");
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    success = true;
                    return response;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "General exception error in HttpClient");
            throw;
        }
        finally
        {
            if (response != null && !success)
                response.Dispose();
        }

        request = null;
        return null;
    }
}