using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Runners;

public class ParallelStepRunnerTest : ParallelStepRunnerTestBase<ParallelStepRunner>
{
    protected override ParallelStepRunner CreateParallelRunner(int workerCount = 2)
    {
        return CreateParallelStepRunner(workerCount);
    }

    private ParallelStepRunner CreateParallelStepRunner(int workerCount = 2)
    {
        return new ParallelStepRunner(workerCount, ServiceProvider);
    }

    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ParallelStepRunner(1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParallelStepRunner(new Random().Next(int.MinValue, 0), ServiceProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParallelStepRunner(0, ServiceProvider));
    }

    [Fact]
    public async Task RunAsync_Cancelled()
    {
        // Have deterministic result
        var runner = CreateParallelRunner(1);

        var cts = new CancellationTokenSource();

        var b = new ManualResetEvent(false);

        StepRunnerErrorEventArgs? error = null!;
        runner.Error += (_, e) =>
        {
            error = e;
        };

        var ran1 = false;
        var step1 = new TestStep(_ =>
        {
            ran1 = true;
            cts.Cancel();
            b.Set();
        }, ServiceProvider);

        var ran2 = false;
        var step2 = new TestStep(_ => ran2 = true, ServiceProvider);

        runner.AddStep(step1);
        runner.AddStep(step2);

        FinishAdding(runner);

        await runner.RunAsync(cts.Token);

        Assert.True(runner.IsCancelled);
        Assert.NotNull(error);
        Assert.True(error.Cancel);
        Assert.True(ran1);
        Assert.False(ran2);
    }
}