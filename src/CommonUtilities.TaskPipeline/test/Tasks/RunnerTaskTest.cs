using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Sklavenwalker.CommonUtilities.TaskPipeline.Tasks;
using Xunit;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Test.Tasks;

public class RunnerTaskTest
{
    [Fact]
    public void Test()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        task.Object.Dispose();
        Assert.True(task.Object.IsDisposed);
    }

    [Fact]
    public void TestRun()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        task.Object.Run(default);

        task.Protected().Verify("RunCore", Times.Exactly(1), false, (CancellationToken)default);
    }

    [Fact]
    public void TestRunWithException()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var e = new Exception();
        task.Protected().Setup("RunCore", false, (CancellationToken)default)
            .Throws(e);

        Assert.Throws<Exception>(() => task.Object.Run(default));
        Assert.Same(e, task.Object.Error);

    }

    [Fact]
    public void TestRunWithCancellation()
    {
        var sc = new ServiceCollection();
        var task = new Mock<RunnerTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        task.Protected().Setup("RunCore", false, cts.Token)
            .Callback<CancellationToken>(d => d.ThrowIfCancellationRequested());

        Assert.Throws<OperationCanceledException>(() => task.Object.Run(cts.Token));
        Assert.Null(task.Object.Error);
    }
}

public class SynchronizedTaskTest
{
    [Fact]
    public void TestWaitThrows()
    {
        var sc = new ServiceCollection();
        var task = new Mock<SynchronizedTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        
        Assert.Throws<TimeoutException>(() => task.Object.Wait(TimeSpan.Zero));
    }

    [Fact]
    public void TestThrowsWait()
    {
        var sc = new ServiceCollection();
        var task = new Mock<SynchronizedTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };


        task.Protected().Setup("SynchronizedInvoke", false, (CancellationToken)default)
            .Callback(() => throw new Exception());

        Assert.Throws<Exception>(() => task.Object.Run(default));

        task.Object.Wait();
    }

    [Fact]
    public void TestCancelled()
    {
        var sc = new ServiceCollection();
        var task = new Mock<SynchronizedTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        var flag = false;
        task.Object.Canceled += delegate
        {
            flag = true;
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();


        task.Protected().Setup("SynchronizedInvoke", false, cts.Token)
            .Callback<CancellationToken>(t => t.ThrowIfCancellationRequested());

        Assert.Throws<OperationCanceledException>(() => task.Object.Run(cts.Token));

        task.Object.Wait();
        Assert.True(flag);
    }

    [Fact]
    public void TestWait()
    {
        var sc = new ServiceCollection();
        var task = new TestSync(sc.BuildServiceProvider());

        Task.Run(() => task.Run(default));
        task.Wait();
        Assert.True(task.Flag);
    }

    private class TestSync : SynchronizedTask
    {
        public bool Flag;
        public TestSync(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override void SynchronizedInvoke(CancellationToken token)
        {
            Task.Delay(500, token);
            Flag = true;
        }
    }

    [Fact]
    public void TestWaitTimeout()
    {
        var sc = new ServiceCollection();
        var task = new Mock<SynchronizedTask>(sc.BuildServiceProvider())
        {
            CallBase = true
        };

        task.Protected().Setup("SynchronizedInvoke", false, (CancellationToken)default)
            .Callback(() =>
            {
                Task.Delay(1000);
            });

        Task.Factory.StartNew(() => task.Object.Run(default), default, TaskCreationOptions.None, TaskScheduler.Default);
        Assert.Throws<TimeoutException>(() => task.Object.Wait(TimeSpan.FromMilliseconds(100)));
    }
}
