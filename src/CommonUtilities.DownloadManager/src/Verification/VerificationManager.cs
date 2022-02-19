using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Verification;

/// <inheritdoc cref="IVerificationManager"/>
public class VerificationManager : IVerificationManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly IDictionary<string, ICollection<IVerifier>> _verifiers;

    /// <summary>
    /// Initializes a new <see cref="VerificationManager"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public VerificationManager(IServiceProvider serviceProvider)
    {
        _verifiers = new Dictionary<string, ICollection<IVerifier>>(StringComparer.OrdinalIgnoreCase);
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public void RegisterVerifier(string extension, IVerifier verifier)
    {
        Requires.NotNullOrEmpty(extension, nameof(extension));
        Requires.NotNull(verifier, nameof(verifier));
        extension = NormalizeExtension(extension);
        if (!_verifiers.TryGetValue(extension, out var verifiers))
            _verifiers[extension] = new List<IVerifier>{ verifier };
        else
            verifiers.Add(verifier);
    }

    /// <inheritdoc/>
    public void RemoveVerifier(string extension, IVerifier verifier)
    {
        Requires.NotNullOrEmpty(extension, nameof(extension));
        Requires.NotNull(verifier, nameof(verifier));
        extension = NormalizeExtension(extension);
        if (_verifiers.TryGetValue(extension, out var verifiers)) 
            verifiers.Remove(verifier);
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream file, VerificationContext verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        if (file is not FileStream fileStream)
            throw new ArgumentException(nameof(file));
        var result = VerificationResult.NotVerified;
        string? path = null;
        if (path is null && _fileSystem.File.Exists(fileStream.Name))
            path = fileStream.Name;
        if (path is null)
            throw new InvalidOperationException();
        try
        {
            var extension = NormalizeExtension(_fileSystem.Path.GetExtension(path));
            var verifiers = GetVerifier(extension);
            if (verifiers is null)
                return VerificationResult.NotVerified;
            foreach (var verifier in verifiers)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                result = verifier.Verify(fileStream, verificationContext);
                if (result != VerificationResult.Success)
                    break;
            }
            if (result == VerificationResult.Success) 
                _logger?.LogTrace($"Verification of {path} successful.");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Cancelled operation during verification.");
            result = VerificationResult.Exception;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, e.Message);
            result = VerificationResult.Exception;
        }

        return result;
    }

    private ICollection<IVerifier>? GetVerifier(string extension)
    {
        ICollection<IVerifier>? verifiers = null;
        if (_verifiers.ContainsKey(extension))
            verifiers = _verifiers[extension];
        else if (_verifiers.ContainsKey("*"))
            verifiers = _verifiers["*"];
        return verifiers;
    }

    private static string NormalizeExtension(string extension)
    {
        return !extension.StartsWith(".", StringComparison.Ordinal) ? extension : extension.TrimStart('.');
    }
}