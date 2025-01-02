using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// A Download manager which supports local file system and HTTP downloads by default.
/// </summary>
public sealed class DownloadManager : IDownloadManager 
{

    private readonly ILogger? _logger;
    private readonly IDownloadManagerConfiguration _configuration;

    private readonly List<IDownloadProvider> _allProviders = [];
    private readonly LeastRecentlyUsedDownloadProviders _leastRecentlyUsedDownloadProviders = new();

    /// <inheritdoc/>
    public IEnumerable<string> Providers => _allProviders.Select(e => e.Name);

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadManager"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider of this instance.</param>
    public DownloadManager(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _configuration = serviceProvider.GetService<IDownloadManagerConfigurationProvider>()?.GetConfiguration() ??
                         DownloadManagerConfiguration.Default;
        switch (_configuration.InternetClient)
        {
            case InternetClient.HttpClient:
                AddDownloadProvider(new HttpClientDownloader(serviceProvider));
                break;
#if !NET
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
        if (provider == null) 
            throw new ArgumentNullException(nameof(provider));
        if (_allProviders.Any(e => string.Equals(e.Name, provider.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Provider " + provider.Name + " already exists.");
        _allProviders.Add(provider);
    }

    /// <inheritdoc/>
    public Task<DownloadResult> DownloadAsync(Uri uri, Stream outputStream, DownloadUpdateCallback? progress,
        IDownloadValidator? validator = null, CancellationToken cancellationToken = default)
    {
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));
        if (!outputStream.CanWrite)
            throw new NotSupportedException("Input stream must be writable.");

        _logger?.LogTrace($"Download requested: {uri.AbsoluteUri}");

        if (uri is { IsFile: false, IsUnc: false })
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
            var providers = GetMatchingProviders(uri);
            return Task.Run(async () =>
                await DownloadWithRetry(providers, uri, outputStream, progress, validator, cancellationToken)
                    .ConfigureAwait(false), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogTrace($"Unable to get download provider: {ex.Message}");
            throw;
        }
    }

    // ReSharper disable once UnusedMember.Global
    internal void RemoveAllEngines()
    {
        _allProviders.Clear();
    }

    private async Task<DownloadResult> DownloadWithRetry(IList<IDownloadProvider> providers, Uri uri, Stream outputStream,
        DownloadUpdateCallback? progress, IDownloadValidator? validator, CancellationToken cancellationToken)
    {
        if (_configuration.ValidationPolicy == ValidationPolicy.Required && validator is null)
        {
            var exception = new NotSupportedException("A validation callback is required for this download.");
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
                        if (progress is null)
                            return;
                        status.DownloadProvider = provider.Name;
                        progress.Invoke(status);
                    }, cancellationToken).ConfigureAwait(false);
                
                if (outputStream.Length == 0 && !_configuration.AllowEmptyFileDownload)
                {
                    var exception = new InvalidOperationException($"Empty file downloaded on '{uri}'.");
                    _logger?.LogError(exception, exception.Message);
                    throw exception;
                }


                if (_configuration.ValidationPolicy == ValidationPolicy.NoValidation)
                {
                    _logger?.LogTrace("Skipping validation because verification context of is not valid.");
                }
                else
                {
                    if (validator is null)
                    {
                        _logger?.LogTrace("Skipping validation because verification context of is not valid.");
                    }
                    else
                    {
                        bool validationSuccess;
                        try
                        {
                            validationSuccess = await validator.Validate(outputStream, summary.DownloadedSize, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            var exception = new DownloadValidationFailedException(
                                $"Validation of '{uri.AbsoluteUri}' failed with exception: {e.Message}", e);
                            _logger?.LogError(exception, exception.Message);
                            throw exception;
                        }

                        if (!validationSuccess)
                        {
                            var exception = new DownloadValidationFailedException(
                                $"Downloaded file '{uri.AbsoluteUri}' is not valid.");
                            _logger?.LogError(exception, exception.Message);
                            throw exception;
                        }
                    }
                }

                _logger?.LogInformation($"Download of '{uri.AbsoluteUri}' succeeded using provider '{provider.Name}'");
                _leastRecentlyUsedDownloadProviders.LastSuccessfulProvider = provider.Name;

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

                await Task.Delay(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken);
            }
        }

        return null!;
    }

    private IList<IDownloadProvider> GetMatchingProviders(Uri uri)
    {
        var source = uri.IsFile || uri.IsUnc ? DownloadKind.File : DownloadKind.Internet;
        var supportedProviders = _allProviders.Where(e => e.IsSupported(source)).ToList();
        if (!supportedProviders.Any())
        {
            _logger?.LogTrace("Unable to find a matching download provider.");
            throw new DownloadProviderNotFoundException("Can not download. No suitable download provider found.");
        }
        return _leastRecentlyUsedDownloadProviders.GetProvidersInPriorityOrder(supportedProviders);
    }
}