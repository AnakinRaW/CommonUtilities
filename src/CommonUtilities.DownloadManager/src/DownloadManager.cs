using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Verification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Download manager which supports local file system and HTTP downloads by default.
/// </summary>
public class DownloadManager : IDownloadManager {

    private readonly ILogger? _logger;
    private readonly IDownloadManagerConfiguration _configuration;

    private readonly List<IDownloadProvider> _allProviders = new();
    private readonly PreferredDownloadProviders _preferredDownloadProviders = new();
    private readonly IVerificationManager _verifier;

    /// <inheritdoc/>
    public IEnumerable<string> Providers => _allProviders.Select(e => e.Name);

    /// <summary>
    /// Creates a new <see cref="DownloadManager"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider of this instance.</param>
    public DownloadManager(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _configuration = serviceProvider.GetService<IDownloadManagerConfiguration>() ??
                         DownloadManagerConfiguration.Default;
        _verifier = serviceProvider.GetRequiredService<IVerificationManager>();
        switch (_configuration.InternetClient)
        {
            case InternetClient.HttpClient:
                AddDownloadProvider(new HttpClientDownloader(serviceProvider));
                break;
#if !NET6_0_OR_GREATER
            case InternetClient.WebClient:
                AddDownloadProvider(new WebClientDownloader(serviceProvider));
                break;
#endif
            default:
                throw new ArgumentOutOfRangeException();
        }
        AddDownloadProvider(new FileDownloader(serviceProvider));
    }

    /// <inheritdoc/>
    public void AddDownloadProvider(IDownloadProvider provider)
    {
        Requires.NotNull(provider, nameof(provider));
        if (_allProviders.Any(e => string.Equals(e.Name, provider.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Provider " + provider.Name + " already exists.");
        _allProviders.Add(provider);
    }

    /// <inheritdoc/>
    public Task<DownloadSummary> DownloadAsync(Uri uri, Stream outputStream, ProgressUpdateCallback? progress,
        IVerificationContext? verificationContext = null, CancellationToken cancellationToken = default)
    {
        _logger?.LogTrace($"Download requested: {uri.AbsoluteUri}");
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));
        if (!outputStream.CanWrite)
            throw new InvalidOperationException("Input stream must be writable.");
        if (!uri.IsFile && !uri.IsUnc)
        {
            if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, "ftp", StringComparison.OrdinalIgnoreCase))
            {
                var argumentException = new ArgumentException($"Uri scheme '{uri.Scheme}' is not supported.");
                _logger?.LogTrace($"Uri scheme '{uri.Scheme}' is not supported. {argumentException.Message}");
                throw argumentException;
            }
            if (uri.AbsoluteUri.Length < 7)
            {
                var argumentException = new ArgumentException($"Invalid Uri: {uri.AbsoluteUri}.");
                _logger?.LogTrace($"The Uri is too short: {uri.AbsoluteUri}; {argumentException.Message}");
                throw argumentException;
            }
        }

        try
        {
            var providers = GetSuitableProvider(uri);
            return Task.Run(async () =>
                await DownloadWithRetry(providers, uri, outputStream, progress, verificationContext, cancellationToken).ConfigureAwait(false), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogTrace($"Unable to get download provider: {ex.Message}");
            throw;
        }
    }

    internal void RemoveAllEngines()
    {
        _allProviders.Clear();
    }

    private async Task<DownloadSummary> DownloadWithRetry(IList<IDownloadProvider> providers, Uri uri, Stream outputStream,
        ProgressUpdateCallback? progress, IVerificationContext? verificationContext, CancellationToken cancellationToken)
    {
        if (_configuration.VerificationPolicy == VerificationPolicy.Enforce && verificationContext is null)
        {
            var exception = new VerificationFailedException(VerificationResult.VerificationContextError,
                "No verification context available to verify the download.");
            _logger?.LogError(exception, exception.Message);
            throw exception;
        }

        var failureList = new List<DownloadFailureInformation>();
        foreach (var provider in providers)
        {
            var position = outputStream.Position;
            var length = outputStream.Length;
            try
            {
                _logger?.LogTrace($"Attempting download '{uri.AbsoluteUri}' using provider '{provider.Name}'");
                var summary = await provider.DownloadAsync(uri, outputStream,
                    status =>
                    {
                        progress?.Invoke(new ProgressUpdateStatus(provider.Name, status.BytesRead, status.TotalBytes, status.BitRate));
                    }, cancellationToken).ConfigureAwait(false);
                if (outputStream.Length == 0 && !_configuration.AllowEmptyFileDownload)
                {
                    var exception = new Exception($"Empty file downloaded on '{uri}'.");
                    _logger?.LogError(exception, exception.Message);
                    throw exception;
                }

                if (_configuration.VerificationPolicy != VerificationPolicy.Skip && verificationContext is not null)
                {
                    var valid = verificationContext.Verify();
                    if (valid)
                    {
                        var verificationResult = _verifier.Verify(outputStream, verificationContext);
                        summary.ValidationResult = verificationResult;
                        if (verificationResult != VerificationResult.Success)
                        {
                            var exception = new VerificationFailedException(verificationResult,
                                $"Verification on downloaded file '{uri.AbsoluteUri}' was not successful.");
                            _logger?.LogError(exception, exception.Message);
                            throw exception;
                        }
                    }
                    else
                    {
                        if (_configuration.VerificationPolicy is VerificationPolicy.Optional or VerificationPolicy.Enforce)
                            throw new VerificationFailedException(VerificationResult.VerificationContextError,
                                "Download is missing or has an invalid VerificationContext");
                        _logger?.LogTrace("Skipping validation because verification context of is not valid.");
                    }
                }

                _logger?.LogInformation($"Download of '{uri.AbsoluteUri}' succeeded using provider '{provider.Name}'");
                _preferredDownloadProviders.LastSuccessfulProviderName = provider.Name;

                summary.DownloadProvider = provider.Name;
                return summary;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failureList.Add(new DownloadFailureInformation(ex, provider.Name));
                _logger?.LogTrace($"Download failed using {provider.Name} provider. {ex}");

                if (provider.Equals(providers.LastOrDefault()))
                    throw new DownloadFailedException(failureList);

                cancellationToken.ThrowIfCancellationRequested();
                if (outputStream.CanSeek)
                {
                    outputStream.SetLength(length);
                    outputStream.Seek(position, SeekOrigin.Begin);
                }
                var millisecondsTimeout = _configuration.DownloadRetryDelay;
                if (millisecondsTimeout <= 0)
                    continue;

                _logger?.LogTrace($"Sleeping {millisecondsTimeout} before retrying download.");
                Thread.Sleep(millisecondsTimeout);
            }
        }

        return null!;
    }

    private IList<IDownloadProvider> GetSuitableProvider(Uri uri)
    {
        var source = uri.IsFile || uri.IsUnc ? DownloadSource.File : DownloadSource.Internet;
        var supportedProviders = _allProviders.Where(e => e.IsSupported(source)).ToList();
        if (!supportedProviders.Any())
        {
            _logger?.LogTrace("Unable to select suitable download provider.");
            throw new DownloadProviderNotFoundException("Can not download. No suitable download provider found.");
        }
        return _preferredDownloadProviders.GetProvidersInPriorityOrder(supportedProviders);
    }
}