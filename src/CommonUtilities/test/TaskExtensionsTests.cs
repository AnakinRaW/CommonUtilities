using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class TaskExtensionsTests
{
    [Fact]
    public void WaitAsyncTResult_TokenThatCannotCancel_ReturnsSourceTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var task = tcs.Task.WaitAsync(CancellationToken.None);

        Assert.Same(tcs.Task, task);
    }

    [Fact]
    public void WaitAsyncTResult_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var token = new CancellationToken(true);
        var task = tcs.Task.WaitAsync(token);

        Assert.True(task.IsCanceled);
        Assert.Equal(token, GetCancellationTokenFromTask(task));
    }

    [Fact]
    public async Task WaitAsyncTResult_TokenCanceled_CancelsTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var cts = new CancellationTokenSource();
        var task = tcs.Task.WaitAsync(cts.Token);
        Assert.False(task.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
    }

    [Fact]
    public void WaitAsync_TokenThatCannotCancel_ReturnsSourceTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var task = ((Task)tcs.Task).WaitAsync(CancellationToken.None);

        Assert.Same(tcs.Task, task);
    }

    [Fact]
    public void WaitAsync_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var token = new CancellationToken(true);
        var task = ((Task)tcs.Task).WaitAsync(token);

        Assert.True(task.IsCanceled);
        Assert.Equal(token, GetCancellationTokenFromTask(task));
    }

    [Fact]
    public async Task WaitAsync_TokenCanceled_CancelsTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var cts = new CancellationTokenSource();
        var task = ((Task)tcs.Task).WaitAsync(cts.Token);
        Assert.False(task.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
    }

    private static CancellationToken GetCancellationTokenFromTask(Task task)
    {
        try
        {
            task.Wait();
        }
        catch (AggregateException ex)
        {
            if (ex.InnerException is OperationCanceledException oce)
                return oce.CancellationToken;
        }
        return CancellationToken.None;
    }
}