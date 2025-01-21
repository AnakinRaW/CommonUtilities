using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Progress;

public class AggregatedProgressReporterTests : CommonTestBase
{
    private readonly TestProgressReporter _internalReporter = new();

    [Fact]
    public void Ctor_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter(null!, []));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter(_internalReporter, null!));

        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter(null!, [], EqualityComparer<TestStep>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter(_internalReporter, null!, EqualityComparer<TestStep>.Default));
        Assert.Throws<ArgumentNullException>(() => new AggregateTestReporter(_internalReporter, [], null!));
    }

    [Fact]
    public void Ctor_SetsProperties()
    {
        var step = new TestStep(1, "Step 1", ServiceProvider);
        var steps = new List<TestStep>
        {
            step,
            step, // Add same reference twice
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        var reporter = new AggregateTestReporter(_internalReporter, steps);

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }

    [Fact]
    public void Ctor_SetsProperties_WithEqualityComparer()
    {
        var step = new TestStep(1, "Step 1", ServiceProvider);
        var other = new TestStep(99, "Step 1", ServiceProvider);
        var steps = new List<TestStep>
        {
            step,
            other, // Add a step that equals 'step'
            new(2, "Step 2", ServiceProvider),
            new(3, "Step 3", ServiceProvider)
        };

        var reporter = new AggregateTestReporter(_internalReporter, steps, new TestStepEqualityComparer());

        Assert.Equal(6, reporter.TotalSize);
        Assert.Equal(3, reporter.TotalStepCount);
    }

    [Fact]
    public void Report_Null_Throws()
    {
        var reporter = new AggregateTestReporter(_internalReporter, []);

        Assert.Throws<ArgumentNullException>(() => reporter.Report(null!, 0.5));
        Assert.Throws<ArgumentNullException>(() => ((IStepProgressReporter)reporter).Report(null!, 0.5));
    }

    [Fact]
    public void Report_InvalidStepType_Throws()
    {
        var reporter = new AggregateTestReporter(_internalReporter, []);

        Assert.Throws<InvalidCastException>(() => 
            ((IStepProgressReporter)reporter).Report(new OtherTestStep(ServiceProvider), 1.0));
    }

    [Fact]
    public void Report_IgnoresUnregisteredStep()
    {
        var step = new TestStep(1, "Step 1", ServiceProvider);

        var reporter = new AggregateTestReporter(_internalReporter, []);
        
        reporter.Report(step, 0.5);

        Assert.Null(_internalReporter.ReportedData);
    }
    
    [Fact]
    public void Report()
    {
        var step1 = new TestStep(1, "Step 1", ServiceProvider);
        var step2 = new TestStep(1, "Step 2", ServiceProvider);

        var reporter = new AggregateTestReporter(_internalReporter, [step1, step2]);

        reporter.Report(step1, 0.5);

        Assert.NotNull(_internalReporter.ReportedData);

        Assert.Equal("Step 1", _internalReporter.ReportedData.Text);
        Assert.Equal("TestType", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(0.5, _internalReporter.ReportedData.Progress);
        Assert.Equal(0.5, _internalReporter.ReportedData.ProgressInfo.Progress);

        ((IStepProgressReporter)reporter).Report(step2, 1);

        Assert.Equal("Step 2", _internalReporter.ReportedData.Text);
        Assert.Equal("TestType", _internalReporter.ReportedData.Type.Id);
        Assert.Equal(1, _internalReporter.ReportedData.Progress);
        Assert.Equal(1, _internalReporter.ReportedData.ProgressInfo.Progress);
    }

    private class TestStepEqualityComparer : EqualityComparer<TestStep>
    {
        public override bool Equals(TestStep? x, TestStep? y)
        {
            return x.Text.Equals(y.Text);
        }

        public override int GetHashCode(TestStep obj)
        {
            return obj.Text.GetHashCode();
        }
    }

    private class AggregateTestReporter : AggregatedProgressReporter<TestStep, TestInfo>
    {
        public AggregateTestReporter(IProgressReporter<TestInfo> progressReporter, IEnumerable<TestStep> steps) 
            : base(progressReporter, steps)
        {
        }

        public AggregateTestReporter(IProgressReporter<TestInfo> progressReporter, IEnumerable<TestStep> steps, IEqualityComparer<TestStep> equalityComparer)
            : base(progressReporter, steps, equalityComparer)
        {
        }

        protected override ProgressType Type => new()
        {
            Id = "TestType",
            DisplayName= "TestType"
        };

        protected override string GetProgressText(TestStep step)
        {
            return step.Text;
        }

        protected override double CalculateAggregatedProgress(TestStep task, double progress, out TestInfo progressInfo)
        {
            progressInfo = new TestInfo();
            progressInfo.Progress = progress;
            return progress;
        }
    }


    public struct TestInfo
    {
        public double Progress;
    }

    private class TestProgressReporter : IProgressReporter<TestInfo>
    {
        public ReportedData? ReportedData { get; private set; }

        public void Report(string progressText, double progress, ProgressType type, TestInfo detailedProgress)
        {
            ReportedData = new ReportedData
            {
                Text = progressText,
                Progress = progress,
                Type = type,
                ProgressInfo = detailedProgress
            };
        }
    }

    private class OtherTestStep : PipelineStep, IProgressStep
    {
        public OtherTestStep(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override void RunCore(CancellationToken token)
        {
        }

        public ProgressType Type => new() { Id = "test", DisplayName = "Test" };
        public IStepProgressReporter ProgressReporter { get; }
        public long Size => 1;
    }

    private class ReportedData
    {
        public string Text { get; init; }
        public double Progress { get; init; }
        public ProgressType Type { get; init; }
        public TestInfo ProgressInfo { get; init; }
    }
}