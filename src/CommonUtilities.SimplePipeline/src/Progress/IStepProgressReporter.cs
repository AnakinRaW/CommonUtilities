namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Receives progress information of an <see cref="IProgressStep"/> which is then reported.
/// </summary>
public interface IStepProgressReporter
{
    /// <summary>
    /// Reports the progress of an <see cref="IProgressStep"/>.
    /// </summary>
    /// <param name="step">The step being reported.</param>
    /// <param name="progress">The current progress value as a percentage, ranging from 0.0 to 1.0.</param>
    void Report(IProgressStep step, double progress);
}