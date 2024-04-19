using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class SynchronizedStepTest
{
    [Fact]
    public void Test_Wait_ThrowsTimeoutException()
    {
        var sc = new ServiceCollection();
        var step = new Mock<SynchronizedStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        Assert.Throws<TimeoutException>(() => step.Object.Wait(TimeSpan.Zero));
    }

    [Fact]
    public void Test_Run_ThrowsWait()
    {
        var sc = new ServiceCollection();
        var step = new Mock<SynchronizedStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };


        step.Protected().Setup("RunSynchronized", false, (CancellationToken)default)
            .Callback(() => throw new Exception());

        Assert.Throws<Exception>(() => step.Object.Run(default));

        step.Object.Wait();
    }

    [Fact]
    public void Test_Run_Cancelled_ThrowsOperationCanceledException()
    {
        var sc = new ServiceCollection();
        var step = new Mock<SynchronizedStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var flag = false;
        step.Object.Canceled += delegate
        {
            flag = true;
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();


        step.Protected().Setup("RunSynchronized", false, cts.Token)
            .Callback<CancellationToken>(t => t.ThrowIfCancellationRequested());

        Assert.Throws<OperationCanceledException>(() => step.Object.Run(cts.Token));

        step.Object.Wait();
        Assert.True(flag);
    }

    [Fact]
    public void Test_Wait()
    {
        var sc = new ServiceCollection();
        var step = new TestSync(sc.BuildServiceProvider());

        Task.Run(() => step.Run(default));
        step.Wait();
        Assert.True(step.Flag);
    }

    private class TestSync : SynchronizedStep
    {
        public bool Flag;
        public TestSync(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override void RunSynchronized(CancellationToken token)
        {
            Task.Delay(500, token);
            Flag = true;
        }
    }

    [Fact]
    public void Test_Wait_WithTimeout()
    {
        var sc = new ServiceCollection();
        var step = new Mock<SynchronizedStep>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        step.Protected().Setup("RunSynchronized", false, (CancellationToken)default)
            .Callback(() =>
            {
                Task.Delay(1000).Wait();
            });

        Task.Factory.StartNew(() => step.Object.Run(default), default, TaskCreationOptions.None, TaskScheduler.Default);
        Assert.Throws<TimeoutException>(() => step.Object.Wait(TimeSpan.FromMilliseconds(100)));
    }
}