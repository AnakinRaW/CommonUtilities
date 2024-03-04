using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Verification.Empty;
using AnakinRaW.CommonUtilities.Verification.Hash;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.Verification;

/// <inheritdoc cref="IVerificationManager"/>
public class VerificationManager : IVerificationManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger? _logger;
    internal readonly IDictionary<Type, List<IVerifier>> Verifiers;

    /// <summary>
    /// Initializes a new <see cref="VerificationManager"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public VerificationManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Verifiers = new Dictionary<Type, List<IVerifier>>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    /// <summary>
    /// Adds common verifiers to this instance
    /// </summary>
    public void AddDefaultVerifiers()
    {
        RegisterVerifier(new HashVerifier(_serviceProvider));
        RegisterVerifier(new EmptyContextVerifier());
    }

    /// <inheritdoc/>
    public void RegisterVerifier<T>(IVerifier<T> verifier) where T : IVerificationContext
    {
        if (verifier == null) 
            throw new ArgumentNullException(nameof(verifier));
        var contextType = typeof(T);
        RegisterVerifier(contextType, verifier);
    }

    private void RegisterVerifier(Type verificationContextType, IVerifier verifier)
    {
        if (verificationContextType == null) 
            throw new ArgumentNullException(nameof(verificationContextType));
        if (verifier == null) 
            throw new ArgumentNullException(nameof(verifier));

        if (!verificationContextType.IsAssignableTo(typeof(IVerificationContext)))
            throw new InvalidCastException($"{verificationContextType.FullName} does not implement {nameof(IVerificationContext)}");
        if (!Verifiers.TryGetValue(verificationContextType, out var verifiers))
            Verifiers[verificationContextType] = new List<IVerifier> { verifier };
        else
            verifiers.Add(verifier);
    }

    /// <inheritdoc/>
    public void RemoveVerifier(IVerifier verifier)
    {
        if (verifier == null) 
            throw new ArgumentNullException(nameof(verifier));

        foreach (var verifiers in Verifiers.Values) 
            verifiers.RemoveAll(v => v == verifier);
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream file, IVerificationContext verificationContext)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));
        return VerifyCore(file, verificationContext);
    }

    /// <inheritdoc/>
    public VerificationResult Verify(IFileInfo file, IVerificationContext verificationContext)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));
        using var stream = file.OpenRead();
        return VerifyCore(stream, verificationContext);
    }

    private VerificationResult VerifyCore(Stream stream, IVerificationContext verificationContext)
    {
        var result = VerificationResult.NotVerified;
        try
        {
            if (!stream.CanRead || !stream.CanSeek)
                throw new InvalidOperationException("Cannot read or seek stream");

            var contextType = verificationContext.GetType();
            var verifiers = GetVerifier(contextType);
            if (verifiers is null)
                return VerificationResult.NotVerified;
            foreach (var verifier in verifiers)
            {
                stream.Seek(0, SeekOrigin.Begin);
                result = verifier.Verify(stream, verificationContext);
                if (result.Status != VerificationResultStatus.Success)
                    break;
            }

            if (result.Status == VerificationResultStatus.Success)
            {
                var path = stream.GetPathFromStream();
                var message = "Verification successful.";
                if (!string.IsNullOrEmpty(path)) 
                    message = $"Verification of {path} successful.";
                _logger?.LogTrace(message);
            }
                
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Cancelled operation during verification.");
            result = VerificationResult.FromError("Verification cancelled by user");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, e.Message);
            result = VerificationResult.FromError(e.Message);
        }

        return result;
    }

    internal ICollection<IVerifier>? GetVerifier(Type contextType)
    {
        Verifiers.TryGetValue(contextType, out var verifiers);
        return verifiers;
    }
}