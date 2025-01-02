using System;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// The exception that is thrown when the <see cref="HashTypeKey"/> specified for a <see cref="IHashingService"/> was not found.
/// </summary>
/// <param name="keyType">The hash key that was not found.</param>
public sealed class HashProviderNotFoundException(HashTypeKey keyType) : Exception
{
    /// <inheritdoc />
    public override string Message => $"Unable to find hash provider for the key {keyType}";
}