using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// The event args that represent an error during the execution of a <see cref="IStepRunner"/>.
/// </summary>
public class StepRunnerErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception of the execution error.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the step that caused the error or <see langword="null"/> if the step runner execution caused an error unrelated to a specific step.
    /// </summary>
    public IStep? Step { get; }

    /// <summary>
    /// Gets a value indicating whether the step was faulted due to cancellation. 
    /// </summary>
    /// <remarks>Once set to <see langword="true"/>, this property cannot be set to <see langword="false"/> again.</remarks>
    public bool Cancel
    {
        get;
        set => field |= value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StepRunnerErrorEventArgs"/> class with the specified step.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="step">The faulted step.</param>
    public StepRunnerErrorEventArgs(Exception exception, IStep? step)
    {
        Exception = exception;
        Step = step;
    }
}