using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Engines;

internal class HttpClientDownloader : DownloadEngineBase
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

    private HttpResponseMessage? GetWebResponse(Uri uri, ref DownloadSummary summary, out HttpResponseMessage? request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}