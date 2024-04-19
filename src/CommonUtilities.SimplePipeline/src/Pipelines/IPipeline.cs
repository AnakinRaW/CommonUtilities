using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A pipeline can run multiple operations in sequence or simultaneously, based on how it was prepared.
/// </summary>
public interface IPipeline : IDisposable
{ 
    /// <summary>
    /// Prepares this instance for execution.
    /// </summary>
    /// <remarks>
    /// Preparation can only be done once per instance.
    /// </remarks>
    /// <returns>A task that represents whether the preparation was successful.</returns>
    Task<bool> PrepareAsync();
    
    /// <summary>
    /// Runs pipeline synchronously.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <returns>A task that represents the operation completion.</returns>
    /// <exception cref="OperationCanceledException">If <paramref name="token"/> was requested for cancellation.</exception>
    /// <exception cref="StepFailureException">The pipeline may throw this exception if one or many steps failed.</exception>
    Task RunAsync(CancellationToken token = default);
}