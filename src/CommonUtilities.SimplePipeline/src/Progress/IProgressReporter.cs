namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Receives progress information of an operation which is then reported.
/// </summary>
/// <typeparam name="T">The type of detailed progress information.</typeparam>
public interface IProgressReporter<in T>
{
    /// <summary>
    /// Reports the current progress of an operation.
    /// </summary>
    /// <param name="progressText">The text description of the current progress.</param>
    /// <param name="progress">The current progress value as a percentage, ranging from 0.0 to 1.0.</param>
    /// <param name="type">The type of progress being reported.</param>
    /// <param name="detailedProgress">Additional progress information.</param>
    void Report(string progressText, double progress, ProgressType type, T? detailedProgress);
}