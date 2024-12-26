using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace AnakinRaW.CommonUtilities.Registry.Extensions;

/// <summary>
/// Provides extension methods to the <see cref="RegistryKey"/> class.
/// </summary>
public static class RegistryKeyExtensions
{
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
    /// <exception cref="ArgumentNullException"><paramref name="registryKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">When this method is invoked with a <see cref="WindowsRegistryKey"/> on a non-Windows platform.</exception>
    public static Task WaitForChangeAsync(
        this IRegistryKey registryKey,
        bool watchSubtree = true,
        RegistryChangeNotificationFilters change =
            RegistryChangeNotificationFilters.Value | RegistryChangeNotificationFilters.Subkey,
        CancellationToken cancellationToken = default)
    {
        if (registryKey == null)
            throw new ArgumentNullException(nameof(registryKey));

        if (registryKey is WindowsRegistryKey windowsRegistryKey)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Registry is not supported on this platform.");

            return WindowsRegistryAwaiter.WaitForRegistryChangeAsync(windowsRegistryKey.WindowsKey.Handle,
                watchSubtree, change, cancellationToken);
        }

        if (registryKey is InMemoryRegistryKey inMemoryRegistryKey)
            return WaitForRegistryKeyChangeAsync(inMemoryRegistryKey, watchSubtree, change, cancellationToken);

        return Task.CompletedTask;
    }

    private static async Task WaitForRegistryKeyChangeAsync(
        InMemoryRegistryKey inMemoryRegistryKey,
        bool watchSubtree = true,
        RegistryChangeNotificationFilters change =
            RegistryChangeNotificationFilters.Value | RegistryChangeNotificationFilters.Subkey,
        CancellationToken cancellationToken = default)
    {
        using var evt = new ManualResetEventSlim();

        try
        {
            InMemoryRegistryKeyData.RegistryChanged += OnRegistryChanged;
            inMemoryRegistryKey.Disposing += OnKeyDisposing;

            // Handle potential race when registering the disposed event
            if (inMemoryRegistryKey.IsDisposed)
                evt.Set();

            await evt.WaitHandle.ToTask(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            InMemoryRegistryKeyData.RegistryChanged -= OnRegistryChanged;
            inMemoryRegistryKey.Disposing -= OnKeyDisposing;
        }

        return;

        void OnRegistryChanged(object sender, InMemoryRegistryChangedEventArgs e)
        {
            if (ShouldNotifyKeyChange(
                    e.KeyData,
                    inMemoryRegistryKey.KeyData,
                    watchSubtree,
                    change,
                    e.Kind))
                evt.Set();
        }

        void OnKeyDisposing(object sender, EventArgs e)
        {
            evt.Set();
        }
    }

    private static bool ShouldNotifyKeyChange(
        InMemoryRegistryKeyData actualKey,
        InMemoryRegistryKeyData keyToObserve, 
        bool watchSubtree,
        RegistryChangeNotificationFilters filter,
        InMemoryRegistryChangeKind changeKind)
    {
        var stringComparison =
            keyToObserve.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var actualKeyName = actualKey.Name.AsSpan();
        var keyNameToObserve = keyToObserve.Name.AsSpan();

        // If the path lengths are equal, they must be equal.
        if (actualKeyName.Length == keyNameToObserve.Length)
        {
            if (FiltersApply(changeKind, filter) || changeKind is InMemoryRegistryChangeKind.TreeDelete)
                return actualKeyName.Equals(keyNameToObserve, stringComparison);
            
            return false;
        }

        // If actualKeyName is shorter than keyNameToObserve it can never be a subKey.
        if (actualKeyName.Length < keyNameToObserve.Length)
            return false;

        var actualKeyRoot = actualKeyName.Slice(0, keyNameToObserve.Length);
        var isChildOf = actualKeyName[keyNameToObserve.Length] == InMemoryRegistryKeyData.Separator &&
               keyNameToObserve.Equals(actualKeyRoot, stringComparison);
        
        if (!isChildOf)
            return false;

        var isDirectChild = actualKeyName.Length >= keyNameToObserve.Length + 1 &&
                            actualKeyName.Slice(keyNameToObserve.Length + 1)
                                .Equals(actualKey.SubName.AsSpan(), stringComparison);

        if (isDirectChild &&
            filter.HasFlag(RegistryChangeNotificationFilters.Subkey) &&
            changeKind == InMemoryRegistryChangeKind.TreeDelete)
            return true;

        return FiltersApply(changeKind, filter) && watchSubtree;
    }

    private static bool FiltersApply(InMemoryRegistryChangeKind changeKind, RegistryChangeNotificationFilters change)
    {
        return changeKind == InMemoryRegistryChangeKind.Value && change.HasFlag(RegistryChangeNotificationFilters.Value) ||
               (changeKind is InMemoryRegistryChangeKind.TreeCreate or InMemoryRegistryChangeKind.TreeDelete && change.HasFlag(RegistryChangeNotificationFilters.Subkey));
    }
}