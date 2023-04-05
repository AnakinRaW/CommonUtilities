using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a step that reports progress.
/// </summary>
public interface IProgressStep : IStep
{
    /// <summary>
    /// Gets the type of progress that this step reports.
    /// </summary>
    ProgressType Type { get; }

    /// <summary>
    /// Gets the progress reporter associated with this step.
    /// </summary>
    public IStepProgressReporter ProgressReporter { get; }

    /// <summary>
    /// Gets the total size of this step, in bytes.
    /// </summary>
    long Size { get; }
}