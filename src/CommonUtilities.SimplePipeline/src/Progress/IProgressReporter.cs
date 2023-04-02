namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public interface IProgressReporter<in T> where T : new()
{
    void Report(string progressText, double progress, ProgressType type, T detailedProgress);
}