using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sklavenwalker.CommonUtilities.DownloadManager.Configuration;
using Sklavenwalker.CommonUtilities.DownloadManager.Providers;
using Sklavenwalker.CommonUtilities.DownloadManager.Verification;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

/// <summary>
/// Download manager which supports local file system and HTTP downloads by default.
/// </summary>
public class DownloadManager : IDownloadManager {

    private readonly ILogger? _logger;
    private readonly IDownloadManagerConfiguration _configuration;

    private readonly List<IDownloadProvider> _allProviders = new();
    private readonly PreferredDownloadProviders _preferredDownloadProviders = new();
    private readonly IVerifier _verifier;

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
        _verifier = serviceProvider.GetRequiredService<IVerifier>();
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
        VerificationContext? verificationContext = null, CancellationToken cancellationToken = default)
    {
        _logger?.LogTrace($"Download requested: {uri.AbsoluteUri}");
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));
        if (!outputStream.CanWrite)
            throw new InvalidOperationException("Input stream must be writable.");
        if (!uri.IsFile && !uri.IsUnc)
        {
            if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) && !string.Equals(uri.Scheme, "ftp", StringComparison.OrdinalIgnoreCase))
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
            var providers = GetSuitableProvider(_allProviders, uri);
            return Task.Factory.StartNew(() => DownloadWithRetry(providers, uri, outputStream, progress,
                    verificationContext, cancellationToken), cancellationToken,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _logger?.LogTrace($"Unable to get download provider: {ex.Message}");
            throw;
        }
    }

    private DownloadSummary DownloadWithRetry(IDownloadProvider[] providers, Uri uri, Stream outputStream,
        ProgressUpdateCallback? progress, VerificationContext? verificationContext, CancellationToken cancellationToken)
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
                var summary = provider.Download(uri, outputStream,
                    status =>
                    {
                        progress?.Invoke(new ProgressUpdateStatus(provider.Name, status.BytesRead, status.TotalBytes, status.BitRate));
                    }, cancellationToken);
                if (outputStream.Length == 0 && !_configuration.AllowEmptyFileDownload)
                {
                    var exception = new Exception($"Empty file downloaded on '{uri}'.");
                    _logger?.LogError(exception, exception.Message);
                    throw exception;
                }

                if (_configuration.VerificationPolicy != VerificationPolicy.Skip && 
                    verificationContext is not null && outputStream.Length != 0)
                {
                    var valid = verificationContext.Verify();
                    if (valid)
                    {
                        var verificationResult = _verifier.Verify(outputStream, verificationContext);
                        summary.ValidationResult = verificationResult;
                        if (verificationResult != VerificationResult.Success)
                        {
                            var exception = new VerificationFailedException(verificationResult,
                                $"Hash on downloaded file '{uri.AbsoluteUri}' does not match expected value.");
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
                outputStream.SetLength(length);
                outputStream.Seek(position, SeekOrigin.Begin);
                var millisecondsTimeout = _configuration.DownloadRetryDelay;
                if (millisecondsTimeout <= 0)
                    continue;

                _logger?.LogTrace($"Sleeping {millisecondsTimeout} before retrying download.");
                Thread.Sleep(millisecondsTimeout);
            }
        }

        return null!;
    }

    private IDownloadProvider[] GetSuitableProvider(IEnumerable<IDownloadProvider> providers, Uri uri)
    {
        var source = uri.IsFile || uri.IsUnc ? DownloadSource.File : DownloadSource.Internet;
        var array = providers.Where(e => e.IsSupported(source)).ToArray();
        if (array.Length == 0)
        {
            _logger?.LogTrace("Unable to select suitable download provider.");
            throw new NoSuitableProviderException("Can not download. No suitable download provider found.");
        }
        return _preferredDownloadProviders.GetProvidersInPriorityOrder(array).ToArray();
    }
}