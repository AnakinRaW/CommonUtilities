using System;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// The exception that is thrown when the <see cref="HashTypeKey"/> specified for a <see cref="IHashingService"/> was not found.
/// </summary>
/// <param name="keyType"></param>
public sealed class HashProviderNotFoundException(HashTypeKey keyType) : Exception
{
    private readonly HashTypeKey _keyType = keyType;

    /// <inheritdoc />
    public override string Message => $"Unable to find hash provider for the key {_keyType}";
}