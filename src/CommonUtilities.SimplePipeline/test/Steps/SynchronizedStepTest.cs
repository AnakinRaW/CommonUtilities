using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Testing;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Steps;

public class SynchronizedStepTest : CommonTestBase
{
    [Fact]
    public void Wait_ThrowsTimeoutException()
    {
        var step = new TestSyncStep(_ => { }, ServiceProvider);
        // Do not run the step
        Assert.Throws<TimeoutException>(() => step.Wait(TimeSpan.Zero));
    }

    [Fact]
    public void Run_ThrowsWait()
    {
        var expectedException = new Exception("Test");
        var step = new TestSyncStep(_ => throw expectedException, ServiceProvider);

        Assert.Throws<Exception>(() => step.Run(CancellationToken.None));

        // Should not block
        step.Wait();
    }

    [Fact]
    public void Run_Cancelled_ThrowsOperationCanceledException()
    {
        var step = new TestSyncStep(ct => { ct.ThrowIfCancellationRequested(); }, ServiceProvider);

        var flag = false;
        step.Canceled += delegate
        {
            flag = true;
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => step.Run(cts.Token));

        // Should not block
        step.Wait();

        Assert.True(flag);
    }

    [Fact]
    public void Wait()
    {
        var flag = false;
        var step = new TestSyncStep(_ =>
        {
            Task.Delay(1000, CancellationToken.None).Wait(CancellationToken.None);
            flag = true;
        }, ServiceProvider);

        Task.Run(() => step.Run(CancellationToken.None)).Forget();
       
        step.Wait();

        Assert.True(flag);
    }

    [Fact]
    public void Wait_WithTimeout()
    {
        var step = new TestSyncStep(_ =>
        {
            Task.Delay(1000, CancellationToken.None).Wait(CancellationToken.None);
        }, ServiceProvider);
        
        Task.Factory.StartNew(() => step.Run(CancellationToken.None), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        Assert.Throws<TimeoutException>(() => step.Wait(TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public void Dispose()
    {
        var step = new TestSyncStep(_ =>
        {
            Task.Delay(1000, CancellationToken.None).Wait(CancellationToken.None);
        }, ServiceProvider);

        step.Dispose(); 
        
        Assert.Throws<ObjectDisposedException>(step.Wait);
    }
}