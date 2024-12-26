using AnakinRaW.CommonUtilities.Registry.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test.Extensions;

// From https://github.com/microsoft/vs-threading

public abstract class RegistryKeyExtensionsTestBase
{
    private const int AsyncDelay = 500;

    protected abstract RegKeyTest CreateTestKey();

    [Fact]
    public async Task AwaitRegKeyChange()
    {
        using var test = CreateTestKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync();
        Assert.False(changeWatcherTask.IsCompleted);
        test.Key.SetValue("a", "b");
        await changeWatcherTask;

        Assert.True(changeWatcherTask.IsCompleted);
    }

    [Fact]
    public async Task AwaitRegKeyChange_CreateOtherUnrelatedKey_DoesNotNotify()
    {
        using var test = CreateTestKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync();
        Assert.False(changeWatcherTask.IsCompleted);

        using var other = CreateTestKey();
        other.Key.CreateSubKey("otherSub");
        
        var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
        Assert.NotSame(changeWatcherTask, completedTask);
    }

    [Fact]
    public async Task AwaitRegKeyChange_SubkeyFilterDoesNotNotifyOnValueChanges()
    {
        using var test = CreateTestKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync(change: RegistryChangeNotificationFilters.Subkey);
        Assert.False(changeWatcherTask.IsCompleted);
        test.Key.SetValue("a", "b");


        var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
        Assert.NotSame(changeWatcherTask, completedTask);
    }


    [Fact]
    public async Task AwaitRegKeyChange_TwoAtOnce_SameKeyHandle()
    {
        var test = CreateTestKey();

        try
        {
            var changeWatcherTask1 = test.Key.WaitForChangeAsync();
            var changeWatcherTask2 = test.Key.WaitForChangeAsync();
            Assert.False(changeWatcherTask1.IsCompleted);
            Assert.False(changeWatcherTask2.IsCompleted);

            test.Key.SetValue("a", "b");
            
            await Task.WhenAll(changeWatcherTask1, changeWatcherTask2);

            Assert.True(changeWatcherTask1.IsCompleted);
            Assert.True(changeWatcherTask2.IsCompleted);
        }
        finally
        {
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_NoChange()
    {
        using var test = CreateTestKey();
        var changeWatcherTask = test.Key.WaitForChangeAsync(cancellationToken: test.FinishedToken);
        Assert.False(changeWatcherTask.IsCompleted);

        // Give a bit of time to confirm the task will not complete.
        var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
        Assert.NotSame(changeWatcherTask, completedTask);
    }

    [Fact]
    public async Task AwaitRegKeyChange_WatchSubtree_Value()
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();
        
        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: true, cancellationToken: test.FinishedToken);

            subKey.SetValue("subkeyValueName", "b");

            await changeWatcherTask;
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_WatchSubtree_Tree()
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: true, cancellationToken: test.FinishedToken);

            subKey.CreateSubKey("subsub");

            await changeWatcherTask;
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Theory]
    [InlineData(RegistryChangeNotificationFilters.Subkey)]
    [InlineData(RegistryChangeNotificationFilters.Value)]
    public async Task AwaitRegKeyChange_SelfDeleted_AlwaysNotifies(RegistryChangeNotificationFilters filter)
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            var changeWatcherTask = subKey.WaitForChangeAsync(watchSubtree: false ,change: filter);
            test.Key.DeleteKey(Path.GetFileName(subKey.Name), false);
            await changeWatcherTask;
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AwaitRegKeyChange_SubKeyCreated_IsAlwaysTriggered(bool watchSubtree)
    {
        var test = CreateTestKey();
        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: watchSubtree, cancellationToken: test.FinishedToken);
            test.CreateSubKey();

            // Because a 1st-level subKey is part of its baseKey, we always track these changes, even if watchSubTree is false
            await changeWatcherTask;
        }
        finally
        {
            test.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AwaitRegKeyChange_SubKeyDeleted_IsAlwaysTriggered(bool watchSubtree)
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: watchSubtree, cancellationToken: test.FinishedToken);
            test.Key.DeleteKey(Path.GetFileName(subKey.Name), false);
            
            // Because a 1st-level subKey is part of its baseKey, we always track these changes, even if watchSubTree is false
            await changeWatcherTask;
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AwaitRegKeyChange_SubKeyDeleted_ValueFilterDoesNotNotify(bool watchSubtree)
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: watchSubtree, RegistryChangeNotificationFilters.Value, cancellationToken: test.FinishedToken);
            test.Key.DeleteKey(Path.GetFileName(subKey.Name), false);

            var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
            Assert.NotSame(changeWatcherTask, completedTask);
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_SubSubKeyDeleted()
    {
        var test = CreateTestKey();
        var subSubKey = test.CreateSubKey("sub").CreateSubKey("subsub");

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: true, cancellationToken: test.FinishedToken);
            test.Key.DeleteKey("sub\\subsub", false);
            await changeWatcherTask;
        }
        finally
        {
            subSubKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_SubSubKeyDeleted_NoWatchSubtree()
    {
        var test = CreateTestKey();
        var subSubKey = test.CreateSubKey("sub").CreateSubKey("subsub");

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: false, cancellationToken: test.FinishedToken);
            test.Key.DeleteKey("sub\\subsub", false);

            var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
            Assert.NotSame(changeWatcherTask, completedTask);
        }
        finally
        {
            subSubKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_ParentKeyDeletedWhileAwaiting()
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            // Only watch for value changes, not tree changes, so we don't notify
            var changeWatcherTask = subKey.WaitForChangeAsync(watchSubtree: false, RegistryChangeNotificationFilters.Value);
            // Delete the parent key
            test.Key.DeleteKey(string.Empty, true);

            // We expect the task to quietly complete (without throwing any exception).
            await changeWatcherTask;
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_NoWatchSubtree()
    {
        var test = CreateTestKey();
        var subKey = test.CreateSubKey();

        try
        {
            var changeWatcherTask = test.Key.WaitForChangeAsync(watchSubtree: false, cancellationToken: test.FinishedToken);

            subKey.SetValue("subkeyValueName", "b");
            subKey.CreateSubKey("subKey");

            // We do not expect changes to sub-keys to complete the task, so give a bit of time to confirm
            // the task doesn't complete.
            var completedTask = await Task.WhenAny(changeWatcherTask, Task.Delay(AsyncDelay));
            Assert.NotSame(changeWatcherTask, completedTask);
        }
        finally
        {
            subKey.Dispose();
            test.Dispose();
        }
    }

    [Fact]
    public async Task AwaitRegKeyChange_Canceled()
    {
        using var test = CreateTestKey();
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

    [Fact]
    public async Task AwaitRegKeyChange_KeyDisposedWhileWatching()
    {
        Task watchingTask;
        using (var test = CreateTestKey())
        {
            watchingTask = test.Key.WaitForChangeAsync();
        }

        // We expect the task to quietly complete (without throwing any exception).
        await watchingTask;
    }

    [Fact]
    public async Task AwaitRegKeyChange_CanceledAndImmediatelyDisposed()
    {
        Task watchingTask;
        CancellationToken expectedCancellationToken;
        using (var test = CreateTestKey())
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

    [Fact]
    public async Task AwaitRegKeyChange_CallingThreadDestroyed()
    {
        var test = CreateTestKey();
        try
        {
            // Start watching and be certain the thread that started watching is destroyed.
            // This simulates a more common case of someone on a threadpool thread watching
            // a key asynchronously and then the .NET Threadpool deciding to reduce the number of threads in the pool.
            Task watchingTask = null!;
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

            await watchingTask;
        }
        finally
        {
            test.Dispose();
        }
    }

    protected class RegKeyTest : IDisposable
    {
        private readonly IRegistry _registry;
        private readonly string _keyName;
        private readonly CancellationTokenSource _testFinished = new();

        public IRegistryKey Key { get; }

        public CancellationToken FinishedToken => _testFinished.Token;

        internal RegKeyTest(IRegistry registry)
        {
            _registry = registry;
            _keyName = "test_" + Path.GetRandomFileName();
            Key = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).CreateSubKey(_keyName)!;
            Assert.NotNull(Key);
        }

        public IRegistryKey CreateSubKey(string? name = null!)
        {
            var key = Key.CreateSubKey(name ?? Path.GetRandomFileName());
            Assert.NotNull(key);
            return key;
        }

        public void Dispose()
        {
            _testFinished.Cancel();
            _testFinished.Dispose();
            Key.Dispose();
            using var key = _registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            key.DeleteKey(_keyName, true);
        }
    }
}