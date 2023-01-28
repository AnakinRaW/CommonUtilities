﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.TaskPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnakinRaW.CommonUtilities.TaskPipeline.Test.Tasks;

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
            .Callback(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
            });

        Task.Factory.StartNew(() => task.Object.Run(default), default, TaskCreationOptions.None, TaskScheduler.Default);
        Assert.Throws<TimeoutException>(() => task.Object.Wait(TimeSpan.FromMilliseconds(100)));
    }
}