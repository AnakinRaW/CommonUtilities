using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
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

public abstract class AggregatedProgressReporterTestBase<T> : CommonTestBase where T : ITestInfo, new()
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
        step.Report( 0.5, "step", CreateCustomProgressInfo(step, 0.5));
        Assert.Null(_internalReporter.ReportedData);
    }

    [Fact]
    public void Report_DefaultT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = new AggregateTestReporter<T>(_internalReporter, [step]);
        step.Report( 0.5, "Text", default);

        Assert.NotNull(_internalReporter.ReportedData);
        Assert.Equal("Step 1aggregated", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType) 
            Assert.Equal(0, _internalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, _internalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(_internalReporter.ReportedData.ProgressInfo!.Aggregated);
    }

    [Fact]
    public void Report_DefaultCustomT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = new AggregateTestReporter<T>(_internalReporter, [step]);

        step.Report(0.5, "Text", CreateCustomProgressInfo(step, 0.5));

        var expected = CreateCustomProgressInfo(step, 0.5);
        expected.Aggregated = true;

        Assert.NotNull(_internalReporter.ReportedData);
        Assert.Equal("Step 1aggregated", _internalReporter.ReportedData.Text);
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

        step1.Report(0.5, "step1", default);

        Assert.NotNull(_internalReporter.ReportedData);

        Assert.Equal("Step 1aggregated", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType)
            Assert.Equal(0, _internalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, _internalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(_internalReporter.ReportedData.ProgressInfo!.Aggregated);

        step2.Report( 1, null, default);

        Assert.Equal("Step 2aggregated", _internalReporter.ReportedData.Text);
        Assert.Equal("test", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(1, _internalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType)
            Assert.Equal(0, _internalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, _internalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(_internalReporter.ReportedData.ProgressInfo!.Aggregated);
    }

    [Fact]
    public void Report_NoReportIfDisposed()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var aggregator = new AggregateTestReporter<T>(_internalReporter, [step]);
        aggregator.Dispose();
        step.Report( 0.5, "step", CreateCustomProgressInfo(step, 0.5));
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

internal class AggregateTestReporter<T> : AggregatedProgressReporter<TestProgressStep<T>, T> where T : ITestInfo, new()
{
    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps)
        : base(progressReporter, steps)
    {
    }

    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps, IEqualityComparer<TestProgressStep<T>> equalityComparer)
        : base(progressReporter, steps, equalityComparer)
    {
    }

    protected override string GetProgressText(TestProgressStep<T> step, string? progressText)
    {
        Assert.Equal("aggregated", progressText);
        return step.Text + progressText;
    }

    protected override ProgressEventArgs<T> CalculateAggregatedProgress(TestProgressStep<T> task, ProgressEventArgs<T> progress)
    {
        var newT = new T
        {
            Aggregated = true,
            Progress = progress.ProgressInfo?.Progress ?? -1
        };
        return new ProgressEventArgs<T>(progress.Progress, "aggregated", newT);
    }
}

internal class TestProgressReporter<T> : IProgressReporter<T>
{
    public ReportedData<T>? ReportedData { get; private set; }

    public void Report(double progress, string? progressText, ProgressType type, T? detailedProgress)
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

internal class ReportedData<T>
{
    public string? Text { get; init; }
    public double Progress { get; init; }
    public ProgressType Type { get; init; }
    public T? ProgressInfo { get; init; }
}