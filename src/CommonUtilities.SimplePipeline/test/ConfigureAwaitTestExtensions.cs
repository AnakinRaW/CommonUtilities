using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test;

// Based on https://github.com/dotnet/runtime TaskAwaiterTests.cs

public static class ConfigureAwaitTestExtensions
{
    public static void AwaiterAndAwaitableEquality<T>(
        Func<T> instanceFactory,
        Func<T, TaskAwaiter> awaitableFactory,
        Func<T, bool, ConfiguredTaskAwaitable> configuredAwaitableFactory)
    {
        var instance = instanceFactory();

        // TaskAwaiter
        Assert.Equal(awaitableFactory(instance), awaitableFactory(instance));

        // ConfiguredTaskAwaitable
        Assert.Equal(configuredAwaitableFactory(instance, false), configuredAwaitableFactory(instance, false));
        Assert.NotEqual(configuredAwaitableFactory(instance, false), configuredAwaitableFactory(instance, true));
        Assert.NotEqual(configuredAwaitableFactory(instance, true), configuredAwaitableFactory(instance, false));

        // ConfiguredTaskAwaitable.ConfiguredTaskAwaiter
        Assert.Equal(configuredAwaitableFactory(instance, false).GetAwaiter(), configuredAwaitableFactory(instance, false).GetAwaiter());
        Assert.NotEqual(configuredAwaitableFactory(instance, false).GetAwaiter(), configuredAwaitableFactory(instance, true).GetAwaiter());
        Assert.NotEqual(configuredAwaitableFactory(instance, true).GetAwaiter(), configuredAwaitableFactory(instance, false).GetAwaiter());
    }

    public static void TestOnCompletedCompletesInAnotherSynchronizationContext<T>(
        bool? continueOnCapturedContext, 
        Func<T> instanceFactory,
        Func<T, TaskAwaiter> awaitableFactory,
        Func<T, bool, ConfiguredTaskAwaitable> configuredAwaitableFactory,
        Action<T> completeInstance)
    {
        var origCtx = SynchronizationContext.Current;
        try
        {
            var validateCtx = new ValidateCorrectContextSynchronizationContext();
            Assert.Equal(0, validateCtx.PostCount);
            SynchronizationContext.SetSynchronizationContext(validateCtx);

            var mre = new ManualResetEventSlim();

            var instance = instanceFactory();

            // Hook up a callback
            var postedInContext = false;
            var callback = () =>
            {
                postedInContext = ValidateCorrectContextSynchronizationContext.IsPostedInContext;
                mre.Set();
            };

            if (continueOnCapturedContext.HasValue)
                configuredAwaitableFactory(instance, continueOnCapturedContext.Value).GetAwaiter().OnCompleted(callback);
            else
                awaitableFactory(instance).OnCompleted(callback);

            Assert.False(mre.IsSet, "Callback should not yet have run.");

            // Complete the task in another context and wait for the callback to run
            Task.Run(() => completeInstance(instance), TestContext.Current.CancellationToken);
            mre.Wait(TestContext.Current.CancellationToken);

            // Validate the callback ran and in the correct context
            var shouldHavePosted = !continueOnCapturedContext.HasValue || continueOnCapturedContext.Value;
            Assert.Equal(shouldHavePosted ? 1 : 0, validateCtx.PostCount);
            Assert.Equal(shouldHavePosted, postedInContext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(origCtx);
        }
    }

    private class ValidateCorrectContextSynchronizationContext : SynchronizationContext
    {
        [ThreadStatic]
        internal static bool IsPostedInContext;

        internal int PostCount;
        private int _sendCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref PostCount);
            Task.Run(() =>
            {
                SetSynchronizationContext(this);
                try
                {
                    IsPostedInContext = true;
                    d(state);
                }
                finally
                {
                    IsPostedInContext = false;
                    SetSynchronizationContext(null);
                }
            });
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _sendCount);
            d(state);
        }
    }
}