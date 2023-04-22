using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Moq;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class AggregatedProgressReporterTests
{
    private readonly Mock<IProgressReporter<TestInfo>> _progressReporter;
    private readonly AggregatedProgressReporter<TestStep, TestInfo> _aggregatedProgressReporter;

    public AggregatedProgressReporterTests()
    {
        _progressReporter = new Mock<IProgressReporter<TestInfo>>();
        _aggregatedProgressReporter = new TestAggregatedProgressReporter(_progressReporter.Object);
    }

    [Fact]
    public void Initialize_UpdatesTotalSize()
    {
        var steps = new List<TestStep>
        {
            new(1, "Step 1"),
            new(2, "Step 2"),
            new(3, "Step 3")
        };

        _aggregatedProgressReporter.Initialize(steps);
        
        Assert.Equal(6, _aggregatedProgressReporter.TotalSize);
    }

    [Fact]
    public void Report_IgnoresUnregisteredStep()
    {
        var step = new TestStep(1, "Step 1");
        _aggregatedProgressReporter.Report(step, 0.5);

        _progressReporter.Verify(
            r => r.Report(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<ProgressType>(), It.IsAny<TestInfo>()),
            Times.Never);
    }

    [Fact]
    public void Report_ReportsProgress()
    {
        var step1 = new TestStep(1, "Step 1");
        var step2 = new TestStep(2, "Step 2");
        var steps = new List<TestStep> { step1, step2 };

        _aggregatedProgressReporter.Initialize(steps);

        _aggregatedProgressReporter.Report(step1, 0.5);

        _progressReporter.Verify(
            r => r.Report("Step 1", It.IsAny<double>(), It.IsAny<ProgressType>(), It.IsAny<TestInfo>()),
            Times.Once);

        _aggregatedProgressReporter.Report(step2, 0.8);

        _progressReporter.Verify(
            r => r.Report("Step 2", It.IsAny<double>(), It.IsAny<ProgressType>(), It.IsAny<TestInfo>()),
            Times.Once);
    }
}

public class TestAggregatedProgressReporter : AggregatedProgressReporter<TestStep, TestInfo>
{
    public TestAggregatedProgressReporter(IProgressReporter<TestInfo> progressReporter) : base(progressReporter)
    {
    }

    protected override ProgressType Type => new() {DisplayName = "Test", Id = "test"};

    protected override double CalculateAggregatedProgress(TestStep task, double progress, ref TestInfo progressInfo)
    {
        return progress;
    }

    protected override string GetProgressText(TestStep step)
    {
        return step.Text;
    }
}

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

public struct TestInfo
{
}