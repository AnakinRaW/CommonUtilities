using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// A wrapper for an <see cref="IParallelStepRunner"/> that can be lazily initialized.
/// </summary>
public sealed class LateInitDelegatingProgressReporter : IStepProgressReporter, IDisposable
{
    private IStepProgressReporter? _innerReporter;

    /// <summary>
    /// Gets a value indicating whether the <see cref="LateInitDelegatingProgressReporter"/> is initialized.
    /// </summary>
    public bool Initialized { get; private set; }

    /// <inheritdoc />
    public void Report(IProgressStep step, double progress)
    {
        _innerReporter?.Report(step, progress);
    }

    /// <summary>
    /// Initializes this wrapper with the actual reporter to report progress to. 
    /// </summary>
    /// <param name="progressReporter">The reporter to report progress to.</param>
    public void Initialize(IStepProgressReporter? progressReporter)
    {
        if (Initialized)
            throw new InvalidOperationException("This LateInitDelegatingProgressReporter is already initialized.");
        _innerReporter = progressReporter;
        Initialized = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_innerReporter is IDisposable disposableReporter)
            disposableReporter.Dispose();
        _innerReporter = null;
    }
}