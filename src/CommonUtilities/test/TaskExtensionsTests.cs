using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class TaskExtensionsTests
{
    [Fact]
    public void Forget()
    {
        Task.FromException(new FormatException()).WaitAsync(new CancellationToken(true)).Forget();
        Task.FromException<int>(new FormatException()).WaitAsync(new CancellationToken(true)).Forget();
        Task.FromCanceled(new CancellationToken(true)).WaitAsync(new CancellationToken(true)).Forget();
        Task.FromCanceled<int>(new CancellationToken(true)).WaitAsync(new CancellationToken(true)).Forget();
        Task.FromResult(42).WaitAsync(new CancellationToken(true)).Forget();
    }

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

    // From .NET Runtime TaskAwaiterTests.cs

    [Fact]
    public static async Task WaitAsync_CanceledAndTimedOut_AlreadyCompleted_UsesTaskResult()
    {
        await Task.CompletedTask.WaitAsync(new CancellationToken(true));
        Assert.Equal(42, await Task.FromResult(42).WaitAsync(new CancellationToken(true)));
        await Assert.ThrowsAsync<FormatException>(() => Task.FromException(new FormatException()).WaitAsync(new CancellationToken(true)));
        await Assert.ThrowsAsync<FormatException>(() => Task.FromException<int>(new FormatException()).WaitAsync(new CancellationToken(true)));
        await Assert.ThrowsAsync<TaskCanceledException>(() => Task.FromCanceled(new CancellationToken(true)).WaitAsync(new CancellationToken(true)));
        await Assert.ThrowsAsync<TaskCanceledException>(() => Task.FromCanceled<int>(new CancellationToken(true)).WaitAsync(new CancellationToken(true)));
    }

    [Fact]
    public static async Task WaitAsync_TimeoutOrCanceled_Throws()
    {
        var tcs = new TaskCompletionSource<int>();
        var cts = new CancellationTokenSource();

        Task assert1 = Assert.ThrowsAsync<TaskCanceledException>(() => ((Task)tcs.Task).WaitAsync(cts.Token));
        Task assert3 = Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task.WaitAsync(cts.Token));
        Assert.False(assert1.IsCompleted);
        Assert.False(assert3.IsCompleted);

        cts.Cancel();
        await Task.WhenAll(assert1, assert3);
    }

    [Fact]
    public static async Task WaitAsync_NoCancellationOrTimeoutOccurs_Success()
    {
        var cts = new CancellationTokenSource();

        var tcsg = new TaskCompletionSource<int>();
        var tg = tcsg.Task.WaitAsync(cts.Token);
        Assert.False(tg.IsCompleted);
        tcsg.SetResult(42);
        Assert.Equal(42, await tg);
    }
}