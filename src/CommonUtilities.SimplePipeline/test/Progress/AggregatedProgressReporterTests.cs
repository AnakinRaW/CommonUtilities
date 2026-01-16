using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Test.TestData;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

#region Test Classes for AggregatedProgressReporter<TStep, TInfo>

public class AggregatedProgressReporterTest_Struct : AggregatedProgressReporterTestBase<TestInfoStruct>
{
    protected override TestInfoStruct CreateCustomProgressInfo(TestProgressStep<TestInfoStruct> step, double progress)
        => new() { Progress = progress };

    protected override ITestableAggregatedReporter CreateReporter(IEnumerable<TestProgressStep<TestInfoStruct>> steps)
        => new AggregateTestReporter<TestInfoStruct>(InternalReporter, steps);

    protected override ITestableAggregatedReporter CreateReporterWithComparer(IEnumerable<TestProgressStep<TestInfoStruct>> steps)
        => new AggregateTestReporter<TestInfoStruct>(InternalReporter, steps, new TestStepEqualityComparer<TestInfoStruct>());

    public override void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoStruct>(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoStruct>(InternalReporter, null!));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoStruct>(null!, [], EqualityComparer<TestProgressStep<TestInfoStruct>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoStruct>(InternalReporter, null!, EqualityComparer<TestProgressStep<TestInfoStruct>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoStruct>(InternalReporter, [], null!));
    }
}

public class AggregatedProgressReporterTest_Class : AggregatedProgressReporterTestBase<TestInfoClass>
{
    protected override TestInfoClass CreateCustomProgressInfo(TestProgressStep<TestInfoClass> step, double progress)
        => new() { Progress = progress };

    protected override ITestableAggregatedReporter CreateReporter(IEnumerable<TestProgressStep<TestInfoClass>> steps)
        => new AggregateTestReporter<TestInfoClass>(InternalReporter, steps);

    protected override ITestableAggregatedReporter CreateReporterWithComparer(IEnumerable<TestProgressStep<TestInfoClass>> steps)
        => new AggregateTestReporter<TestInfoClass>(InternalReporter, steps, new TestStepEqualityComparer<TestInfoClass>());

    public override void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoClass>(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoClass>(InternalReporter, null!));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoClass>(null!, [], EqualityComparer<TestProgressStep<TestInfoClass>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoClass>(InternalReporter, null!, EqualityComparer<TestProgressStep<TestInfoClass>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter<TestInfoClass>(InternalReporter, [], null!));
    }
}

#endregion

#region Test Classes for AggregatedProgressReporter<TInfo>

public class AggregatedProgressReporterSimpleTest_Struct : AggregatedProgressReporterTestBase<TestInfoStruct>
{
    protected override TestInfoStruct CreateCustomProgressInfo(TestProgressStep<TestInfoStruct> step, double progress)
        => new() { Progress = progress };

    protected override ITestableAggregatedReporter CreateReporter(IEnumerable<TestProgressStep<TestInfoStruct>> steps)
        => new AggregateTestReporterSimple<TestInfoStruct>(InternalReporter, steps);

    protected override ITestableAggregatedReporter CreateReporterWithComparer(IEnumerable<TestProgressStep<TestInfoStruct>> steps)
        => new AggregateTestReporterSimple<TestInfoStruct>(InternalReporter, steps, new TestProgressStepEqualityComparer<TestInfoStruct>());

    public override void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoStruct>(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoStruct>(InternalReporter, null!));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoStruct>(null!, [], EqualityComparer<IProgressStep<TestInfoStruct>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoStruct>(InternalReporter, null!, EqualityComparer<IProgressStep<TestInfoStruct>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoStruct>(InternalReporter, [], null!));
    }
}

public class AggregatedProgressReporterSimpleTest_Class : AggregatedProgressReporterTestBase<TestInfoClass>
{
    protected override TestInfoClass CreateCustomProgressInfo(TestProgressStep<TestInfoClass> step, double progress)
        => new() { Progress = progress };

    protected override ITestableAggregatedReporter CreateReporter(IEnumerable<TestProgressStep<TestInfoClass>> steps)
        => new AggregateTestReporterSimple<TestInfoClass>(InternalReporter, steps);

    protected override ITestableAggregatedReporter CreateReporterWithComparer(IEnumerable<TestProgressStep<TestInfoClass>> steps)
        => new AggregateTestReporterSimple<TestInfoClass>(InternalReporter, steps, new TestProgressStepEqualityComparer<TestInfoClass>());

    public override void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoClass>(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoClass>(InternalReporter, null!));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoClass>(null!, [], EqualityComparer<IProgressStep<TestInfoClass>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoClass>(InternalReporter, null!, EqualityComparer<IProgressStep<TestInfoClass>>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporterSimple<TestInfoClass>(InternalReporter, [], null!));
    }
}

#endregion

#region Base Test Class

public abstract class AggregatedProgressReporterTestBase<T> : TestBaseWithServiceProvider where T : ITestInfo, new()
{
    protected readonly TestProgressReporter<T> InternalReporter = new();

    protected abstract T CreateCustomProgressInfo(TestProgressStep<T> step, double progress);
    protected abstract ITestableAggregatedReporter CreateReporter(IEnumerable<TestProgressStep<T>> steps);
    protected abstract ITestableAggregatedReporter CreateReporterWithComparer(IEnumerable<TestProgressStep<T>> steps);

    [Fact]
    public abstract void Ctor_NullArgs_Throws();

    [Fact]
    public void Ctor_SetsProperties()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var steps = new List<TestProgressStep<T>>
        {
            step,
            step,
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        using var reporter = CreateReporter(steps);

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }

    [Fact]
    public void Ctor_SetsProperties_EmptySteps()
    {
        using var reporter = CreateReporter([]);

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
            other,
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        using var reporter = CreateReporterWithComparer(steps);

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }

    [Fact]
    public void Report_IgnoresUnregisteredStep()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = CreateReporter([]);
        step.Report(0.5, "step", CreateCustomProgressInfo(step, 0.5));
        Assert.Null(InternalReporter.ReportedData);
    }

    [Fact]
    public void Report_DefaultT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var r = CreateReporter([step]);
        step.Report(0.5, "Text", default);

        Assert.NotNull(InternalReporter.ReportedData);
        Assert.Equal("Step 1aggregated", InternalReporter.ReportedData.Text);
        Assert.Equal("test", InternalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, InternalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType)
            Assert.Equal(0, InternalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, InternalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(InternalReporter.ReportedData.ProgressInfo!.Aggregated);
    }

    [Fact]
    public void Report_DefaultCustomT()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        _ = CreateReporter([step]);

        step.Report(0.5, "Text", CreateCustomProgressInfo(step, 0.5));

        var expected = CreateCustomProgressInfo(step, 0.5);
        expected.Aggregated = true;

        Assert.NotNull(InternalReporter.ReportedData);
        Assert.Equal("Step 1aggregated", InternalReporter.ReportedData.Text);
        Assert.Equal("test", InternalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, InternalReporter.ReportedData.Progress);
        Assert.Equal(expected, InternalReporter.ReportedData.ProgressInfo);
    }

    [Fact]
    public void Report()
    {
        var step1 = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var step2 = new TestProgressStep<T>(1, "Step 2", ServiceProvider);

        _ = CreateReporter([step1, step2]);

        step1.Report(0.5, "step1", default);

        Assert.NotNull(InternalReporter.ReportedData);
        Assert.Equal("Step 1aggregated", InternalReporter.ReportedData.Text);
        Assert.Equal("test", InternalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, InternalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType)
            Assert.Equal(0, InternalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, InternalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(InternalReporter.ReportedData.ProgressInfo!.Aggregated);

        step2.Report(1, null, default);

        Assert.Equal("Step 2aggregated", InternalReporter.ReportedData.Text);
        Assert.Equal("test", InternalReporter.ReportedData.Type.Id);
        Assert.Equal(1, InternalReporter.ReportedData.Progress);
        if (typeof(T).IsValueType)
            Assert.Equal(0, InternalReporter.ReportedData.ProgressInfo!.Progress);
        else
            Assert.Equal(-1, InternalReporter.ReportedData.ProgressInfo!.Progress);
        Assert.True(InternalReporter.ReportedData.ProgressInfo!.Aggregated);
    }

    [Fact]
    public void Report_NoReportIfDisposed()
    {
        var step = new TestProgressStep<T>(1, "Step 1", ServiceProvider);
        var aggregator = CreateReporter([step]);
        aggregator.Dispose();
        step.Report(0.5, "step", CreateCustomProgressInfo(step, 0.5));
        Assert.Null(InternalReporter.ReportedData);
    }
}

#endregion

#region Test Infrastructure

public interface ITestableAggregatedReporter : IDisposable
{
    long TotalSize { get; }
    int TotalStepCount { get; }
}

public class TestStepEqualityComparer<T> : EqualityComparer<TestProgressStep<T>>
{
    public override bool Equals(TestProgressStep<T>? x, TestProgressStep<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Text.Equals(y.Text);
    }

    public override int GetHashCode(TestProgressStep<T> obj) => obj.Text.GetHashCode();
}

public class TestProgressStepEqualityComparer<T> : EqualityComparer<IProgressStep<T>>
{
    public override bool Equals(IProgressStep<T>? x, IProgressStep<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x is TestProgressStep<T> tx && y is TestProgressStep<T> ty && tx.Text.Equals(ty.Text);
    }

    public override int GetHashCode(IProgressStep<T> obj) =>
        obj is TestProgressStep<T> t ? t.Text.GetHashCode() : obj.GetHashCode();
}

public class AggregateTestReporter<T> : AggregatedProgressReporter<TestProgressStep<T>, T>, ITestableAggregatedReporter
    where T : ITestInfo, new()
{
    long ITestableAggregatedReporter.TotalSize => TotalSize;
    int ITestableAggregatedReporter.TotalStepCount => TotalStepCount;

    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps)
        : base(progressReporter, steps) { }

    public AggregateTestReporter(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps, IEqualityComparer<TestProgressStep<T>> equalityComparer)
        : base(progressReporter, steps, equalityComparer) { }

    protected override string GetProgressText(TestProgressStep<T> step, string? progressText)
    {
        Assert.Equal("aggregated", progressText);
        return step.Text + progressText;
    }

    protected override ProgressEventArgs<T> CalculateAggregatedProgress(TestProgressStep<T> task, ProgressEventArgs<T> progress)
    {
        var newT = new T { Aggregated = true, Progress = progress.ProgressInfo?.Progress ?? -1 };
        return new ProgressEventArgs<T>(progress.Progress, "aggregated", newT);
    }
}

public class AggregateTestReporterSimple<T> : AggregatedProgressReporter<T>, ITestableAggregatedReporter
    where T : ITestInfo, new()
{
    long ITestableAggregatedReporter.TotalSize => TotalSize;
    int ITestableAggregatedReporter.TotalStepCount => TotalStepCount;

    public AggregateTestReporterSimple(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps)
        : base(progressReporter, steps) { }

    public AggregateTestReporterSimple(IProgressReporter<T> progressReporter, IEnumerable<TestProgressStep<T>> steps, IEqualityComparer<IProgressStep<T>> equalityComparer)
        : base(progressReporter, steps, equalityComparer) { }

    protected override string GetProgressText(IProgressStep<T> step, string? progressText)
    {
        Assert.Equal("aggregated", progressText);
        return ((TestProgressStep<T>)step).Text + progressText;
    }

    protected override ProgressEventArgs<T> CalculateAggregatedProgress(IProgressStep<T> task, ProgressEventArgs<T> progress)
    {
        var newT = new T { Aggregated = true, Progress = progress.ProgressInfo?.Progress ?? -1 };
        return new ProgressEventArgs<T>(progress.Progress, "aggregated", newT);
    }
}

public class TestProgressReporter<T> : IProgressReporter<T>
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

public class ReportedData<T>
{
    public string? Text { get; init; }
    public double Progress { get; init; }
    public ProgressType Type { get; init; }
    public T? ProgressInfo { get; init; }
}

#endregion