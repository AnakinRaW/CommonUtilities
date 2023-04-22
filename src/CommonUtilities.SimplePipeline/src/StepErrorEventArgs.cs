using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// <see cref="EventArgs"/> for faulted <see cref="IStep"/>.
/// </summary>
public class StepErrorEventArgs : EventArgs
{
    private bool _cancel;

    /// <summary>
    /// The faulted Step
    /// </summary>
    public IStep Step { get; }

    /// <summary>
    /// Indicates whether the step was faulted due to cancellation. 
    /// </summary>
    /// <remarks>Once set to <see langword="true"/>, this property cannot be set to <see langword="false"/> again.</remarks>
    public bool Cancel
    {
        get => _cancel;
        set => _cancel |= value;
    }


    /// <summary>
    /// Initializes a new instance of <see cref="StepErrorEventArgs"/>.
    /// </summary>
    /// <param name="step">The faulted step.</param>
    public StepErrorEventArgs(IStep step)
    {
        Step = step;
    }
}