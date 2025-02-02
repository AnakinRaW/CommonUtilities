using System.Threading.Tasks;
#if !NET6_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Provides extension methods for the <see cref="Task"/> and <see cref="Task{T}"/> types.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Consumes a task and doesn't do anything with it. Useful for fire-and-forget calls to async methods within async methods.
    /// </summary>
    /// <param name="task">The task whose result is to be ignored.</param>
    public static void Forget(this Task? task)
    {
    }

#if !NET6_0_OR_GREATER

    /// <summary>
    /// Gets a <see cref="Task{TResult}"/> that will complete when this <see cref="Task{TResult}"/> completes
    /// or when the specified <see cref="CancellationToken"/> has cancellation requested.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by this <see cref="Task{TResult}"/>.</typeparam>
    /// <param name="task">The task to wait for. May not be <c>null</c>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous wait. It may or may not be the same instance as the current instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            return task;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult>(cancellationToken);
        return DoWaitAsync(task, cancellationToken);
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that will complete when this Task completes or when the specified <see cref="CancellationToken"/> has cancellation requested.
    /// </summary>
    /// <param name="task">The task to wait for. May not be <c>null</c>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous wait. It may or may not be the same instance as the current instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            return task;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        return DoWaitAsync(task, cancellationToken);
    }

    private static async Task DoWaitAsync(Task task, CancellationToken cancellationToken)
    {
        using var cancelTaskSource = new CancellationTokenTaskSource<EmptyStruct>(cancellationToken);
        await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
    {
        using var cancelTaskSource = new CancellationTokenTaskSource<TResult>(cancellationToken);
        return await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
    }

    internal sealed class CancellationTokenTaskSource<T> : IDisposable
    {
        private readonly IDisposable? _registration;

        public Task<T> Task { get; }

        public CancellationTokenTaskSource(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Task = System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
                return;
            }
            var tcs = new TaskCompletionSource<T>();
            _registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
            Task = tcs.Task;
        }

        public void Dispose()
        {
            _registration?.Dispose();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct EmptyStruct;

#endif
}