using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using System;
using System.Threading;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

public class TestStep : PipelineStep
{
    private readonly Action<CancellationToken>? _action;

    protected TestStep(IServiceProvider sp) : base(sp)
    {
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


public class TestProgressStep<T>(long size, string text, IServiceProvider serviceProvider) 
    : TestStep(serviceProvider), IProgressStep<T>
{
    public event EventHandler<ProgressEventArgs<T>>? Progress;
    
    public long Size { get; } = size;

    public string Text { get; } = text;

    public ProgressType Type => new() { Id = "test", DisplayName = "Test" };

    public void Report(string text, double progress, T? value)
    {
        Progress?.Invoke(this, new ProgressEventArgs<T>(text, progress, value));
    }
}

public struct TestInfoStruct
{
    public double Progress;
}

public class TestInfoClass
{
    public double Progress;
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