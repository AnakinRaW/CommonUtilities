using System;

namespace AnakinRaW.CommonUtilities.Registry;

internal sealed class InMemoryRegistryChangedEventArgs(InMemoryRegistryKeyData key, InMemoryRegistryChangeKind kind) 
    : EventArgs
{
    public InMemoryRegistryKeyData KeyData { get; } = key;
    public InMemoryRegistryChangeKind Kind { get; } = kind;
}