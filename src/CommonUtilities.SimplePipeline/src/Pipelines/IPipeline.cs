using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents an execution pipeline can run multiple <see cref="IStep"/> instanes.
/// </summary>
public interface IPipeline : IDisposable
{ 
    /// <summary>
    /// Prepares the pipeline for execution.
    /// </summary>
    /// <remarks>
    /// Preparation can only be done once per instance.
    /// </remarks>
    /// <returns>A task that completes when the preparation is completed.</returns>
    Task PrepareAsync();
    
    /// <summary>
    /// Runs pipeline synchronously.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <returns>A task that represents the operation completion.</returns>
    /// <exception cref="OperationCanceledException">The pipeline was cancelled was requested for cancellation.</exception>
    /// <exception cref="StepFailureException">The pipeline may throw this exception if one or many steps failed.</exception>
    Task RunAsync(CancellationToken token = default);

    /// <summary>
    /// Cancels the execution of the pipeline.
    /// </summary>
    void Cancel();
}