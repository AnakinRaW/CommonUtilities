#if !NET5_0_OR_GREATER
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
    // From https://github.com/dotnet/runtime
    public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        if (process == null)
            throw new ArgumentNullException(nameof(process));

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
        void Handler(object o, EventArgs eventArgs) => tcs.TrySetResult(default);
        try
        {
            process.Exited += Handler!;
            if (process.HasExited)
                return;

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
                await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            process.Exited -= Handler!;
        }
    }

    private readonly struct EmptyStruct;
}
#endif