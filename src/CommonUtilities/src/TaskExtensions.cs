using System;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Asynchronously waits for the task to complete, or for the cancellation token to be canceled.
    /// </summary>
    /// <param name="task">The task to wait for. May not be <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token that cancels the wait.</param>
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (!cancellationToken.CanBeCanceled)
            return task;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        return DoWaitAsync(task, cancellationToken);
    }

    private static async Task DoWaitAsync(Task task, CancellationToken cancellationToken)
    {
        using var cancelTaskSource = new CancellationTokenTaskSource<object>(cancellationToken);
        await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
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
}