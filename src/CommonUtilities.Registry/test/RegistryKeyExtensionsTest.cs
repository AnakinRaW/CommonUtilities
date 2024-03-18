using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Win32;
using Xunit;
#if NET
using System.Runtime.Versioning;
#endif

namespace AnakinRaW.CommonUtilities.Registry.Test;

#if NET
[SupportedOSPlatform("windows")]
#endif
public class RegistryKeyExtensionsTest
{
    private const int AsyncDelay = 500;

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange()
    {
        using var test = new RegKeyTest();
        var changeWatcherTask = test.Key.WaitForChangeAsync();
        Assert.False(changeWatcherTask.IsCompleted);
        test.Key.SetValue("a", "b");
        await changeWatcherTask;

        Assert.True(changeWatcherTask.IsCompleted);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_TwoAtOnce_SameKeyHandle()
    {
        using var test = new RegKeyTest();
        var changeWatcherTask1 = test.Key.WaitForChangeAsync();
        var changeWatcherTask2 = test.Key.WaitForChangeAsync();
        Assert.False(changeWatcherTask1.IsCompleted);
        Assert.False(changeWatcherTask2.IsCompleted);
        test.Key.SetValue("a", "b");
        await Task.WhenAll(changeWatcherTask1, changeWatcherTask2);

        Assert.True(changeWatcherTask1.IsCompleted);
        Assert.True(changeWatcherTask2.IsCompleted);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_NoChange()
    {
        using var test = new RegKeyTest();
        var changeWatcherTask = test.Key.WaitForChangeAsync(cancellationToken: test.FinishedToken);
        Assert.False(changeWatcherTask.IsCompleted);

        // Give a bit of time to confirm the task will not complete.
        var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
        Assert.NotSame(changeWatcherTask, completedTask);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_WatchSubtree()
    {
        using var test = new RegKeyTest();
        using var subKey = test.CreateSubKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: true, cancellationToken: test.FinishedToken);
        subKey.SetValue("subkeyValueName", "b");
        await changeWatcherTask;
    }
    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_KeyDeleted()
    {
        using var test = new RegKeyTest();
        using var subKey = test.CreateSubKey();
        var changeWatcherTask = subKey.WaitForChangeAsync(watchSubtree: true, cancellationToken: test.FinishedToken);
        test.Key.DeleteSubKey(Path.GetFileName(subKey.Name));
        await changeWatcherTask;
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_NoWatchSubtree()
    {
        using var test = new RegKeyTest();
        using var subKey = test.CreateSubKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: false, cancellationToken: test.FinishedToken);
        subKey.SetValue("subkeyValueName", "b");

        // We do not expect changes to sub-keys to complete the task, so give a bit of time to confirm
        // the task doesn't complete.
        var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
        Assert.NotSame(changeWatcherTask, completedTask);
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_Canceled()
    {
        using var test = new RegKeyTest();
        var cts = new CancellationTokenSource();
        var changeWatcherTask = test.Key.WaitForChangeAsync(cancellationToken: cts.Token);
        Assert.False(changeWatcherTask.IsCompleted);
        cts.Cancel();
        try
        {
            await changeWatcherTask;
            Assert.Fail("Expected exception not thrown.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.Equal(cts.Token, ex.CancellationToken);
        }
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_KeyDisposedWhileWatching()
    {
        Task watchingTask;
        using (var test = new RegKeyTest())
        {
            watchingTask = test.Key.WaitForChangeAsync();
        }

        // We expect the task to quietly complete (without throwing any exception).
        await watchingTask;
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_CanceledAndImmediatelyDisposed()
    {
        Task watchingTask;
        CancellationToken expectedCancellationToken;
        using (var test = new RegKeyTest())
        {
            expectedCancellationToken = test.FinishedToken;
            watchingTask = test.Key.WaitForChangeAsync(cancellationToken: test.FinishedToken);
        }

        try
        {
            await watchingTask;
            Assert.Fail("Expected exception not thrown.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.Equal(expectedCancellationToken, ex.CancellationToken);
        }
    }

    [PlatformSpecificFact(TestPlatformIdentifier.Windows)]
    public async Task AwaitRegKeyChange_CallingThreadDestroyed()
    {
        using var test = new RegKeyTest();
        // Start watching and be certain the thread that started watching is destroyed.
        // This simulates a more common case of someone on a threadpool thread watching
        // a key asynchronously and then the .NET Threadpool deciding to reduce the number of threads in the pool.
        Task? watchingTask = null;
        var thread = new Thread(() =>
        {
            watchingTask = test.Key.WaitForChangeAsync(cancellationToken: test.FinishedToken);
        });
        thread.Start();
        thread.Join();

        // Verify that the watching task is still watching.
        var completedTask = await Task.WhenAny(watchingTask!, Task.Delay(AsyncDelay));
        Assert.NotSame(watchingTask, completedTask);
        test.CreateSubKey().Dispose();
        await watchingTask!;
    }

#if NET
    [SupportedOSPlatform("windows")]
#endif
    private class RegKeyTest : IDisposable
    {
        private readonly string _keyName;
        private readonly RegistryKey _key;
        private readonly CancellationTokenSource _testFinished = new();

        internal RegKeyTest()
        {
            _keyName = "test_" + Path.GetRandomFileName();
            _key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_keyName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile);
        }

        public RegistryKey Key => _key;

        public CancellationToken FinishedToken => _testFinished.Token;

        public RegistryKey CreateSubKey(string? name = null)
        {
            return _key.CreateSubKey(name ?? Path.GetRandomFileName(), RegistryKeyPermissionCheck.Default, RegistryOptions.Volatile);
        }

        public void Dispose()
        {
            _testFinished.Cancel();
            _testFinished.Dispose();
            _key.Dispose();
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_keyName);
        }
    }
}