using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Extension methods and awaitables for .NET types.
/// </summary>
public static class AwaitExtensions
{
    // From https://github.com/dotnet/runtime
    /// <summary>
    /// Returns a task that completes when the process exits and provides the exit code of that process.
    /// </summary>
    /// <param name="process">The process to wait for exit.</param>
    /// <param name="cancellationToken">
    /// A token whose cancellation will cause the returned Task to complete
    /// before the process exits in a faulted state with an <see cref="OperationCanceledException"/>.
    /// This token has no effect on the <paramref name="process"/> itself.
    /// </param>
    /// <returns>A task whose result is the <see cref="Process.ExitCode"/> of the <paramref name="process"/>.</returns>
    public static
#if !NET
        async
#endif

        Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        if (process == null)
            throw new ArgumentNullException(nameof(process));

#if NET
        return process.WaitForExitAsync(cancellationToken);
#else
        if (!process.HasExited)
            cancellationToken.ThrowIfCancellationRequested();

        try
        {
            process.EnableRaisingEvents = true;
        }
        catch (InvalidOperationException)
        {
            if (process.HasExited)
                return;
            throw;
        }

        var tcs = new TaskCompletionSource<EmptyStruct>();
        try
        {
            process.Exited += Handler;
            if (process.HasExited)
                return;
#if NETSTANDARD2_1
            await
#endif
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
                await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            process.Exited -= Handler;
        }

        return;

        void Handler(object o, EventArgs eventArgs) => tcs.TrySetResult(default);
#endif

    }

    private readonly struct EmptyStruct;
}