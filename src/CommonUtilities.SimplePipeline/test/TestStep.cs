using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class TestStep : IProgressStep
{
    public ProgressType Type => new() { Id = "test", DisplayName = "Test" };

    public IStepProgressReporter ProgressReporter { get; }

    public long Size { get; }

    public string Text { get; }

    public Exception Error { get; }

    public TestStep(long size, string text)
    {
        Size = size;
        Text = text;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Run(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}