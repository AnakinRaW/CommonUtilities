﻿using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AnakinRaW.CommonUtilities.Registry.Windows;

// From https://github.com/microsoft/vs-threading
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