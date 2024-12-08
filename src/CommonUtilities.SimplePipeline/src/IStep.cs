using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A step tha can be queued to an <see cref="IRunner"/> and executes a custom action.
/// </summary>
public interface IStep : IDisposable
{
    /// <summary>
    /// The exception, if any, that happened while running this step.
    /// </summary>
    Exception? Error { get; }

    /// <summary>
    /// Run the step's action.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow step cancellation.</param>
    void Run(CancellationToken token);
}