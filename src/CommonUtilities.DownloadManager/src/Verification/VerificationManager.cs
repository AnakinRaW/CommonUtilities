using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.DownloadManager.Verification;

/// <inheritdoc cref="IVerificationManager"/>
public class VerificationManager : IVerificationManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    internal readonly IDictionary<string, List<IVerifier>> Verifiers;

    /// <summary>
    /// Initializes a new <see cref="VerificationManager"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public VerificationManager(IServiceProvider serviceProvider)
    {
        Verifiers = new Dictionary<string, List<IVerifier>>(StringComparer.OrdinalIgnoreCase);
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <inheritdoc/>
    public void RegisterVerifier(string extension, IVerifier verifier)
    {
        Requires.NotNullOrWhiteSpace(extension, nameof(extension));
        Requires.NotNull(verifier, nameof(verifier));
        extension = NormalizeExtension(extension);
        if (!Verifiers.TryGetValue(extension, out var verifiers))
            Verifiers[extension] = new List<IVerifier>{ verifier };
        else
            verifiers.Add(verifier);
    }

    /// <inheritdoc/>
    public void RemoveVerifier(string extension, IVerifier verifier)
    {
        Requires.NotNullOrWhiteSpace(extension, nameof(extension));
        Requires.NotNull(verifier, nameof(verifier));
        extension = NormalizeExtension(extension);
        if (Verifiers.TryGetValue(extension, out var verifiers))
        {
            verifiers.RemoveAll(v => v == verifier);
            if (verifiers.Count == 0)
                Verifiers.Remove(extension);
        }
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream file, IVerificationContext verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        if (file is not FileStream fileStream)
            throw new ArgumentException(nameof(file));
        var path = fileStream.Name;
        return Verify(fileStream, path, verificationContext);
    }

    /// <inheritdoc/>
    public VerificationResult Verify(IFileInfo file, IVerificationContext verificationContext)
    {
        Requires.NotNull(file, nameof(file));
        var stream = file.OpenRead();
        var path = file.FullName;
        return Verify(stream, path, verificationContext);
    }

    private VerificationResult Verify(Stream stream, string path, IVerificationContext verificationContext)
    {
        Requires.NotNullOrEmpty(path, nameof(path));
        var result = VerificationResult.NotVerified;
        if (!_fileSystem.File.Exists(path))
            throw new FileNotFoundException();
        try
        {
            var extension = NormalizeExtension(_fileSystem.Path.GetExtension(path));
            var verifiers = GetVerifier(extension);
            if (verifiers is null)
                return VerificationResult.NotVerified;
            foreach (var verifier in verifiers)
            {
                stream.Seek(0, SeekOrigin.Begin);
                result = verifier.Verify(stream, verificationContext);
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
        if (Verifiers.ContainsKey(extension))
            verifiers = Verifiers[extension];
        else if (Verifiers.ContainsKey("*"))
            verifiers = Verifiers["*"];
        return verifiers;
    }

    private static string NormalizeExtension(string extension)
    {
        return !extension.StartsWith(".", StringComparison.Ordinal) ? extension : extension.TrimStart('.');
    }
}