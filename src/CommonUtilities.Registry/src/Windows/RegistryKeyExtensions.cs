using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Windows;

/// <summary>
/// Provides extension methods to the <see cref="RegistryKey"/> class.
/// </summary>
#if NET8_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public static class RegistryKeyExtensions
{
    private static readonly Version Windows8Version = new(6, 2, 9200);
    private static bool IsWindows8OrLater => Environment.OSVersion.Platform == PlatformID.Win32NT
                                             && Environment.OSVersion.Version >= Windows8Version;

    /// <summary>
    /// Returns a Task that completes when the specified registry key changes.
    /// </summary>
    /// <param name="registryKey">The registry key to watch for changes.</param>
    /// <param name="watchSubtree"><c>true</c> to watch the keys descendent keys as well;
    /// <c>false</c> to watch only this key without descendents.</param>
    /// <param name="change">Indicates the kinds of changes to watch for.</param>
    /// <param name="cancellationToken">A token that may be canceled to release the resources from watching
    /// for changes and complete the returned Task as canceled.</param>
    /// <returns>
    /// A task that completes when the registry key changes, the handle is closed, or upon cancellation.
    /// </returns>
    public static Task WaitForChangeAsync(this RegistryKey registryKey, bool watchSubtree = true,
        RegistryChangeNotificationFilters change =
            RegistryChangeNotificationFilters.Value | RegistryChangeNotificationFilters.Subkey,
        CancellationToken cancellationToken = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Registry is not supported on this platform.");

        if (registryKey == null)
            throw new ArgumentNullException(nameof(registryKey));
        return WaitForRegistryChangeAsync(registryKey.Handle, watchSubtree, change, cancellationToken);
    }

    private static async Task WaitForRegistryChangeAsync(SafeRegistryHandle registryKeyHandle, bool watchSubtree,
        RegistryChangeNotificationFilters change, CancellationToken cancellationToken)
    {
        IDisposable? dedicatedThreadReleaser = null;
        try
        {
            using var evt = new ManualResetEventSlim();

            void RegisterAction()
            {
                var win32Error = Advapi32.RegNotifyChangeKeyValue(registryKeyHandle, watchSubtree, change, evt.WaitHandle.SafeWaitHandle, true);
                if (win32Error != 0)
                {
                    throw new Win32Exception(win32Error);
                }
            }

            if (IsWindows8OrLater)
            {
                change |= Advapi32.RegNotifyThreadAgnostic;
                RegisterAction();
            }
            else
            {
                // Engage our downlevel support by using a single, dedicated thread to guarantee
                // that we request notification on a thread that will not be destroyed later.
                // Although we *could* await this, we synchronously block because our caller expects
                // subscription to have begun before we return: for the async part to simply be notification.
                // This async method we're calling uses .ConfigureAwait(false) internally so this won't
                // deadlock if we're called on a thread with a single-thread SynchronizationContext.
                dedicatedThreadReleaser = DownlevelRegistryWatcherSupport
                    .ExecuteOnDedicatedThreadAsync(RegisterAction).GetAwaiter().GetResult();
            }

            await evt.WaitHandle.ToTask(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            dedicatedThreadReleaser?.Dispose();
        }
    }


    private static class DownlevelRegistryWatcherSupport
    {
        private const int SmallThreadStackSize = 100 * 1024;

        private static readonly object SyncObject = new();
        private static readonly Queue<Tuple<Action, TaskCompletionSource<EmptyStruct>>> PendingWork = new();
        private static int _keepAliveCount;
        private static Thread? _liveThread;


        internal static async Task<IDisposable> ExecuteOnDedicatedThreadAsync(Action action)
        {
            if (action == null) 
                throw new ArgumentNullException(nameof(action));

            var tcs = new TaskCompletionSource<EmptyStruct>();
            var keepAliveCountIncremented = false;
            try
            {
                lock (SyncObject)
                {
                    PendingWork.Enqueue(Tuple.Create(action, tcs));

                    try
                    {
                        // This block intentionally left blank.
                    }
                    finally
                    {
                        // We make these two assignments within a finally block
                        // to guard against an untimely ThreadAbortException causing
                        // us to execute just one of them.
                        keepAliveCountIncremented = true;
                        ++_keepAliveCount;
                    }

                    if (_keepAliveCount == 1)
                    {
                        if (_liveThread is not null)
                            throw new InvalidOperationException();

                        _liveThread = new Thread(Worker, SmallThreadStackSize)
                        {
                            IsBackground = true,
                            Name = "Registry watcher",
                        };
                        _liveThread.Start();
                    }
                    else
                    {
                        // There *could* temporarily be multiple threads in some race conditions.
                        // Pulse all of them so that the live one is sure to get the message.
                        Monitor.PulseAll(SyncObject);
                    }
                }

                await tcs.Task.ConfigureAwait(false);
                return new ThreadHandleRelease();
            }
            catch
            {
                if (keepAliveCountIncremented)
                {
                    // Our caller will never have a chance to release their claim on the dedicated thread,
                    // so do it for them.
                    ReleaseRefOnDedicatedThread();
                }

                throw;
            }
        }

        private static void ReleaseRefOnDedicatedThread()
        {
            lock (SyncObject)
            {
                if (--_keepAliveCount == 0)
                {
                    _liveThread = null;

                    // Wake up any obsolete thread(s) so they can go to exit.
                    Monitor.PulseAll(SyncObject);
                }
            }
        }

        private static void Worker()
        {
            while (true)
            {
                Tuple<Action, TaskCompletionSource<EmptyStruct>>? work = null;
                lock (SyncObject)
                {
                    if (Thread.CurrentThread != _liveThread)
                    {
                        // Regardless of our PendingWork and keepAliveCount,
                        // it isn't meant for this thread any more.
                        // This happens when keepAliveCount (at least temporarily)
                        // hits 0, so this thread must be assumed to be on its exit path,
                        // and another thread will be spawned to process new requests.
                        if (_liveThread is not object && (_keepAliveCount != 0 || PendingWork.Count != 0))
                            throw new InvalidOperationException();

                        return;
                    }

                    if (PendingWork.Count > 0)
                    {
                        work = PendingWork.Dequeue();
                    }
                    else if (_keepAliveCount == 0)
                    {
                        // No work, and no reason to stay alive. Exit the thread.
                        return;
                    }
                    else
                    {
                        // Sleep until another thread wants to wake us up with a Pulse.
                        Monitor.Wait(SyncObject);
                    }
                }

                if (work is object)
                {
                    try
                    {
                        work.Item1();
                        work.Item2.SetResult(EmptyStruct.Instance);
                    }
                    catch (Exception ex)
                    {
                        work.Item2.SetException(ex);
                    }
                }
            }
        }

        private class ThreadHandleRelease : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                lock (SyncObject)
                {
                    if (_disposed)
                        return;
                    _disposed = true;
                    ReleaseRefOnDedicatedThread();
                }
            }
        }
    }
}

// From https://github.com/microsoft/vs-threading
/// <summary>
/// The various types of data within a registry key that generate notifications
/// when changed.
/// </summary>
/// <remarks>
/// This enum matches the Win32 REG_NOTIFY_CHANGE_* constants.
/// </remarks>
[Flags]
public enum RegistryChangeNotificationFilters
{
    /// <summary>
    /// Notify the caller if a subkey is added or deleted.
    /// Corresponds to Win32 value REG_NOTIFY_CHANGE_NAME.
    /// </summary>
    Subkey = 0x1,

    /// <summary>
    /// Notify the caller of changes to the attributes of the key,
    /// such as the security descriptor information.
    /// Corresponds to Win32 value REG_NOTIFY_CHANGE_ATTRIBUTES.
    /// </summary>
    Attributes = 0x2,

    /// <summary>
    /// Notify the caller of changes to a value of the key. This can
    /// include adding or deleting a value, or changing an existing value.
    /// Corresponds to Win32 value REG_NOTIFY_CHANGE_LAST_SET.
    /// </summary>
    Value = 0x4,

    /// <summary>
    /// Notify the caller of changes to the security descriptor of the key.
    /// Corresponds to Win32 value REG_NOTIFY_CHANGE_SECURITY.
    /// </summary>
    Security = 0x8,
}

internal readonly struct EmptyStruct
{
    internal static EmptyStruct Instance => default;
}

internal static class Advapi32
{
    internal const RegistryChangeNotificationFilters RegNotifyThreadAgnostic = (RegistryChangeNotificationFilters)0x10000000L;

    [DllImport("Advapi32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern int RegNotifyChangeKeyValue(
        SafeRegistryHandle hKey,
        [MarshalAs(UnmanagedType.Bool)] bool watchSubtree,
        RegistryChangeNotificationFilters notifyFilter,
        SafeWaitHandle hEvent,
        [MarshalAs(UnmanagedType.Bool)] bool asynchronous);
}

internal static class TplExtensions
{
    public static readonly Task<bool> TrueTask = Task.FromResult(true);

    public static readonly Task<bool> FalseTask = Task.FromResult(false);

    internal static Task<bool> ToTask(this WaitHandle handle, int timeout = Timeout.Infinite,
        CancellationToken cancellationToken = default)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));

        // Check whether the handle is already signaled as an optimization.
        // But even for WaitOne(0) the CLR can pump messages if called on the UI thread, which the caller may not
        // be expecting at this time, so be sure there is no message pump active by controlling the SynchronizationContext.
        using (NoMessagePumpSyncContext.Default.Apply())
        {
            if (handle.WaitOne(0))
            {
                return TrueTask;
            }

            if (timeout == 0)
            {
                return FalseTask;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var tcs = new TaskCompletionSource<bool>();

        // Arrange that if the caller signals their cancellation token that we complete the task
        // we return immediately. Because of the continuation we've scheduled on that task, this
        // will automatically release the wait handle notification as well.
        var cancellationRegistration =
            cancellationToken.Register(
                state =>
                {
                    var (taskCompletionSource, token) =
                        (Tuple<TaskCompletionSource<bool>, CancellationToken>)state!;
                    taskCompletionSource.TrySetCanceled(token);
                },
                Tuple.Create(tcs, cancellationToken));

        var callbackHandle = ThreadPool.RegisterWaitForSingleObject(
            handle,
            (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
            tcs,
            timeout,
            true);

        // It's important that we guarantee that when the returned task completes (whether cancelled, timed out, or signaled)
        // that we release all resources.
        if (cancellationToken.CanBeCanceled)
        {
            // We have a cancellation token registration and a wait handle registration to release.
            // Use a tuple as a state object to avoid allocating delegates and closures each time this method is called.
            tcs.Task.ContinueWith(
                (_, state) =>
                {
                    var tuple = (Tuple<RegisteredWaitHandle, CancellationTokenRegistration>)state!;
                    tuple.Item1.Unregister(null); // release resources for the async callback
                    tuple.Item2.Dispose(); // release memory for cancellation token registration
                },
                Tuple.Create(callbackHandle, cancellationRegistration),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else
        {
            // Since the cancellation token was the default one, the only thing we need to track is clearing the RegisteredWaitHandle,
            // so do this such that we allocate as few objects as possible.
            tcs.Task.ContinueWith(
                (_, state) => ((RegisteredWaitHandle)state!).Unregister(null),
                callbackHandle,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        return tcs.Task;
    }
}

internal class NoMessagePumpSyncContext : SynchronizationContext
{
    public NoMessagePumpSyncContext()
    {
        // This is required so that our override of Wait is invoked.
        SetWaitNotificationRequired();
    }

    public static SynchronizationContext Default { get; } = new NoMessagePumpSyncContext();

    public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
    {
        if (waitHandles == null)

            throw new ArgumentNullException(nameof(waitHandles));
        // On .NET Framework we must take special care to NOT end up in a call to CoWait (which lets in RPC calls).
        // Off Windows, we can't p/invoke to kernel32, but it appears that .NET Core never calls CoWait, so we can rely on default behavior.
        // We're just going to use the OS as the switch instead of the framework so that (one day) if we drop our .NET Framework specific target,
        // and if .NET Core ever adds CoWait support on Windows, we'll still behave properly.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Kernel32.WaitForMultipleObjects((uint)waitHandles.Length, waitHandles, waitAll,
                (uint)millisecondsTimeout);
        }

        return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
    }
}

internal static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern int WaitForMultipleObjects(uint handleCount, IntPtr[] waitHandles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint millisecondsTimeout);
}


internal static class ThreadingTools
{
    public static SpecializedSyncContext Apply(this SynchronizationContext? syncContext,
        bool checkForChangesOnRevert = true)
    {
        return SpecializedSyncContext.Apply(syncContext, checkForChangesOnRevert);
    }
}

internal readonly struct SpecializedSyncContext : IDisposable
{
    private readonly bool _initialized;
    private readonly SynchronizationContext? _prior;
    private readonly SynchronizationContext? _appliedContext;
    private readonly bool _checkForChangesOnRevert;

    private SpecializedSyncContext(SynchronizationContext? syncContext, bool checkForChangesOnRevert)
    {
        _initialized = true;
        _prior = SynchronizationContext.Current;
        _appliedContext = syncContext;
        _checkForChangesOnRevert = checkForChangesOnRevert;
        SynchronizationContext.SetSynchronizationContext(syncContext);
    }

    public static SpecializedSyncContext Apply(SynchronizationContext? syncContext, bool checkForChangesOnRevert = true)
    {
        return new SpecializedSyncContext(syncContext, checkForChangesOnRevert);
    }

    public void Dispose()
    {
        if (!_initialized)
            return;
        if (_checkForChangesOnRevert && SynchronizationContext.Current != _appliedContext)
            throw new InvalidOperationException();

        SynchronizationContext.SetSynchronizationContext(_prior);
    }
}