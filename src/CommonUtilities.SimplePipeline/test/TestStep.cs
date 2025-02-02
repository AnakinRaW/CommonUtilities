using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class TestStep : PipelineStep, IProgressStep
{
    private readonly Action<CancellationToken>? _action;
    public ProgressType Type => new() { Id = "test", DisplayName = "Test" };

    public IStepProgressReporter ProgressReporter { get; } = null!;

    public long Size { get; }

    public string Text { get; }

    public TestStep(long size, string text, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Size = size;
        Text = text;
    }

    public TestStep(Action<CancellationToken> action, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _action = action;
    }

    protected override void RunCore(CancellationToken token)
    {
        _action?.Invoke(token);
    }
}

public class TestSyncStep(Action<CancellationToken> action, IServiceProvider serviceProvider)
    : SynchronizedStep(serviceProvider)
{
    public ProgressType Type => new() { Id = "test", DisplayName = "Test" };

    protected override void RunSynchronized(CancellationToken token)
    {
        action?.Invoke(token);
    }
}