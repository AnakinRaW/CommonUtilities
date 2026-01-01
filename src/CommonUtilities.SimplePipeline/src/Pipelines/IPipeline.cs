using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents an execution pipeline that can be prepared and run.
/// </summary>
public interface IPipeline : IDisposable
{
    /// <summary>
    /// Prepares the pipeline for execution.
    /// </summary>
    /// <param name="token">Token to cancel the preparation.</param>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task PrepareAsync(CancellationToken token = default);

    /// <summary>
    /// Runs the pipeline asynchronously.
    /// </summary>
    /// <param name="token">Token to cancel the execution.</param>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task RunAsync(CancellationToken token = default);

    /// <summary>
    /// Cancels the execution of the pipeline.
    /// </summary>
    void Cancel();
}