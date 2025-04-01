using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class AggregatedProgressReporterTest_Struct : AggregatedProgressReporterTestBase<TestInfoStruct>
{
    protected override TestInfoStruct CreateCustomProgressInfo(TestProgressStep<TestInfoStruct> step, double progress)
    {
        return new TestInfoStruct
        {
            Progress = progress,
        };
    }
}

public class AggregatedProgressReporterTest_Class: AggregatedProgressReporterTestBase<TestInfoClass>
{
    protected override TestInfoClass CreateCustomProgressInfo(TestProgressStep<TestInfoClass> step, double progress)
    {
        return new TestInfoClass
        {
            Progress = progress,
        };
    }
}

public abstract class AggregatedProgressReporterTestBase<T> : CommonTestBase
{
    private readonly TestProgressReporter<T> _internalReporter = new();

    protected abstract T CreateCustomProgressInfo(TestProgressStep<T> step, double progress);

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<T>(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<T>(_internalReporter, null!));

        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<T>(null!, [], EqualityComparer<TestStep>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<T>(_internalReporter, null!, EqualityComparer<TestStep>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<T>(_internalReporter, [], null!));
    }

    [Fact]
    public void Ctor_SetsProperties()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var steps = new List<TestProgressStep<T>>
        {
            step,
            step, // Add same reference twice
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        using var reporter = new AggregateTestReporter<T>(_internalReporter, steps);

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }


    [Fact]
    public void Ctor_SetsProperties_EmptySteps()
    {
        using var reporter = new AggregateTestReporter<T>(_internalReporter, []);

        Assert.Equal(0, reporter.TotalSize);
        Assert.Equal(0, reporter.TotalStepCount);
    }

    [Fact]
    public void Ctor_SetsProperties_WithEqualityComparer()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var other = new TestProgressStep<T>(99, "Step 1", ServiceProvider);
        var steps = new List<TestProgressStep<T>>
        {
            step,
            other, // Add a step that equals 'step'
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        using var reporter = new AggregateTestReporter<T>(_internalReporter, steps, new TestStepEqualityComparer<T>());

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }

    [Fact]
    public void Report_IgnoresUnregisteredStep()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = new AggregateTestReporter<T>(_internalReporter, []);
        step.Report("step", 0.5, CreateCustomProgressInfo(step, 0.5));
        Assert.Null(_internalReporter.ReportedData);
    }

    [Fact]
    public void Report_DefaultT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = new AggregateTestReporter<T>(_internalReporter, [step]);
        step.Report("Text", 0.5, default);

        Assert.NotNull(_internalReporter.ReportedData);
        Assert.Equal("Step 1Text", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        Assert.Equal(default, _internalReporter.ReportedData.ProgressInfo);
    }

    [Fact]
    public void Report_DefaultCustomT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = new AggregateTestReporter<T>(_internalReporter, [step]);

        var expected = CreateCustomProgressInfo(step, 0.5);
        step.Report("Text", 0.5, expected);

        Assert.NotNull(_internalReporter.ReportedData);
        Assert.Equal("Step 1Text", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        Assert.Equal(expected, _internalReporter.ReportedData.ProgressInfo);
    }

    [Fact]
    public void Report()
    {
        var step1 = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var step2 = new TestProgressStep<T>(1, "Step 2", ServiceProvider);

        _ = new AggregateTestReporter<T>(_internalReporter, [step1, step2]);

        step1.Report("step1", 0.5, default);

        Assert.NotNull(_internalReporter.ReportedData);

        Assert.Equal("Step 1step1", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        Assert.Equal(default, _internalReporter.ReportedData.ProgressInfo);

        step2.Report("step2", 1, default);

        Assert.Equal("Step 2step2", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(1, _internalReporter.ReportedData.Progress);
        Assert.Equal(default, _internalReporter.ReportedData.ProgressInfo);
    }

    [Fact]
    public void Report_NoReportIfDisposed()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var aggregator = new AggregateTestReporter<T>(_internalReporter, [step]);
        aggregator.Dispose();
        step.Report("step", 0.5, CreateCustomProgressInfo(step, 0.5));
        Assert.Null(_internalReporter.ReportedData);
    }
}

internal class TestStepEqualityComparer<T> : EqualityComparer<TestProgressStep<T>>
{
    public override bool Equals(TestProgressStep<T>? x, TestProgressStep<T>? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;
        return x.Text.Equals(y.Text);
    }

    public override int GetHashCode(TestProgressStep<T> obj)
    {
        return obj.Text.GetHashCode();
    }
}

internal class AggregateTestReporter<T> : AggregatedProgressReporter<TestProgressStep<T>, T>
{
    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps)
        : base(progressReporter, steps)
    {
    }

    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps, IEqualityComparer<TestProgressStep<T>> equalityComparer)
        : base(progressReporter, steps, equalityComparer)
    {
    }

    protected override string GetProgressText(TestProgressStep<T> step, string progressText)
    {
        return step.Text + progressText;
    }

    protected override ProgressEventArgs<T> CalculateAggregatedProgress(TestProgressStep<T> task, ProgressEventArgs<T> progress)
    {
        return progress;
    }
}

internal class TestProgressReporter<T> : IProgressReporter<T>
{
    public ReportedData<T>? ReportedData { get; private set; }

    public void Report(string progressText, double progress, ProgressType type, T detailedProgress)
    {
        ReportedData = new ReportedData<T>
        {
            Text = progressText,
            Progress = progress,
            Type = type,
            ProgressInfo = detailedProgress
        };
    }
}

internal class OtherTestStep(IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider), IProgressStep<object?>
{
    public event EventHandler<ProgressEventArgs<object?>>? Progress;
    public ProgressType Type => new() { Id = "test", DisplayName = "Test" };
    public long Size => 1;

    protected override void RunCore(CancellationToken token)
    {
    }
}

internal class ReportedData<T>
{
    public string Text { get; init; }
    public double Progress { get; init; }
    public ProgressType Type { get; init; }
    public T ProgressInfo { get; init; }
}