using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// Provides or delegates to an implementation to calculate hash values. 
/// </summary>
/// <remarks>
/// Instead of using class directly, consider using <see cref="IHashingService"/>. Register this class to the application's <see cref="IServiceProvider"/>
/// using the <see cref="IHashAlgorithmProvider"/> interface type.
/// </remarks>
public interface IHashAlgorithmProvider
{
    /// <summary>
    /// The <see cref="HashTypeKey"/> this class supports.
    /// </summary>
    HashTypeKey SupportedHashType { get; }

    /// <summary>
    /// Computes the hash of data using the algorithm specified in <see cref="SupportedHashType"/>.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> does not support reading.</exception>
    int HashData(ReadOnlySpan<byte> source, Span<byte> destination);

    /// <summary>
    /// Computes the hash of a stream using the algorithm specified in <see cref="SupportedHashType"/>.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> does not support reading.</exception>
    int HashData(Stream source, Span<byte> destination);

    /// <summary>
    /// Asynchronously computes the hash of a stream using the algorithm specified in <see cref="SupportedHashType"/>.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="cancellation">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> does not support reading.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellation = default);
}