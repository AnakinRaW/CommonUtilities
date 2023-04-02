namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public interface IStepProgressReporter
{
    void Report(IProgressStep step, double progress);
}