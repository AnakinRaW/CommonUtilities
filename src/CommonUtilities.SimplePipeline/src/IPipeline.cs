using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A pipeline can be planned and then runs one or many operations.
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
    /// Runs this pipeline.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    void Run(CancellationToken token = default);
}