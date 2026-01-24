using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A step tha can be queued to an <see cref="IStepRunner"/> and executes a custom action.
/// </summary>
public interface IStep : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the step has been cancelled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the step was cancelled; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsCancelled { get; }

    /// <summary>
    /// Gets the exception that occurred during the execution of the step, if any.
    /// </summary>
    /// <value>
    /// An <see cref="Exception"/> representing the error that occurred during the step's execution, 
    /// or <see langword="null"/> if no error occurred.
    /// </value>
    Exception? Error { get; }

    /// <summary>
    /// Run the step's action.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow step cancellation.</param>
    Task RunAsync(CancellationToken token);

    /// <summary>
    /// Gets an awaiter used to await this <see cref="IStep"/>.
    /// </summary>
    TaskAwaiter GetAwaiter();

    /// <summary>
    /// Configures an awaiter used to await this <see cref="IStep"/>.
    /// </summary>
    /// <param name="continueOnCapturedContext"></param>
    /// <returns>An object used to await this task.</returns>
    ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext);
}