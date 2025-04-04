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

    public void Report(double progress, string? text, T? value)
    {
        Progress?.Invoke(this, new ProgressEventArgs<T>(progress, text, value));
    }
}

public interface ITestInfo
{
    double Progress { get; set; }
    bool Aggregated { get; set; }
}

public struct TestInfoStruct : ITestInfo
{
    public double Progress { get; set; }
    public bool Aggregated { get; set; }
}

public class TestInfoClass : ITestInfo, IEquatable<ITestInfo>
{
    public double Progress { get; set; }
    public bool Aggregated { get; set; }

    public bool Equals(ITestInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Progress.Equals(other.Progress) && Aggregated == other.Aggregated;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TestInfoClass)obj);
    }

    private bool Equals(TestInfoClass other)
    {
        return Progress.Equals(other.Progress) && Aggregated == other.Aggregated;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Progress.GetHashCode() * 397) ^ Aggregated.GetHashCode();
        }
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