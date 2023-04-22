using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// Base implementation for any <see cref="IVerifier{T}"/>.
/// </summary>
/// <typeparam name="T">The </typeparam>
public abstract class VerifierBase<T> : IVerifier<T> where T : IVerificationContext
{
    /// <summary>
    ///  The logger for this instance.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// The <see cref="IServiceProvider"/> for this instance.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Initializes a new verifier instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected VerifierBase(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream data, T verificationContext)
    {
        Requires.NotNull(data, nameof(data));
        Requires.NotNullAllowStructs(verificationContext, nameof(verificationContext));
        try
        {
            if (!verificationContext.Verify())
                return new VerificationResult(VerificationResultStatus.VerificationContextError);
            return VerifyCore(data, verificationContext);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, e.Message);
            return new VerificationResult(VerificationResultStatus.Exception)
            {
                Message = e.Message
            };
        }
    }

    VerificationResult IVerifier.Verify(Stream data, IVerificationContext verificationContext)
    {
        // Even though IVerifier<T> is not contravariant, it should be safe to do a simple cast. 
        return Verify(data, (T)verificationContext);
    }

    /// <summary>
    /// Verifies the given stream against the given context <paramref name="verificationContext"/>
    /// </summary>
    /// <remarks>Exceptions are already handled.</remarks>
    /// <param name="data">The data to verify.</param>
    /// <param name="verificationContext">The verification context.</param>
    /// <returns>Status information of the verification.</returns>
    protected abstract VerificationResult VerifyCore(Stream data, T verificationContext);
}