using System;
using System.Threading;

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
    /// <returns><see langword="true"/> if the preparation was successful; <see langword="false"/> otherwise.</returns>
    bool Prepare();

    /// <summary>
    /// Runs pipeline.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <exception cref="OperationCanceledException">If <see cref="token"/> was requested for cancellation.</exception>
    /// <exception cref="StepFailureException">The pipeline may throw this exception if one or many steps failed.</exception>
    void Run(CancellationToken token = default);
}