using System;

namespace AnakinRaW.CommonUtilities.Hashing;

public sealed class HashProviderNotFoundException(HashTypeKey keyType) : Exception
{
    private readonly HashTypeKey _keyType = keyType;

    public override string Message => $"Unable to find hash provider for the key {_keyType}";
}