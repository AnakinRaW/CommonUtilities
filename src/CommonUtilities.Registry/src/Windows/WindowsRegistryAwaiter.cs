﻿using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System;
using AnakinRaW.CommonUtilities.Registry.Extensions;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Windows;

// From https://github.com/microsoft/vs-threading

#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
internal static class WindowsRegistryAwaiter
{
    private static readonly Version Windows8Version = new(6, 2, 9200);
    private static bool IsWindows8OrLater => Environment.OSVersion.Platform == PlatformID.Win32NT
                                             && Environment.OSVersion.Version >= Windows8Version;

    internal static async Task WaitForRegistryChangeAsync(SafeRegistryHandle registryKeyHandle, bool watchSubtree,
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