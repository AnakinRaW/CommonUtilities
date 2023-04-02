using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

public interface IProgressStep : IStep
{
    ProgressType Type { get; }

    public IStepProgressReporter ProgressReporter { get; }

    long Size { get; }
}