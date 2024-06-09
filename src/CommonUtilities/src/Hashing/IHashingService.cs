using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// Service to calculate hash values.
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Computes the hash of a file using the specified algorithm.
    /// </summary>
    /// <param name="file">The file to hash.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The hash of the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    byte[] GetHash(IFileInfo file, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a file using the specified algorithm.
    /// </summary>
    /// <param name="file">The file to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentException">The buffer in destination is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    int GetHash(IFileInfo file, Span<byte> destination, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a file using the specified algorithm.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The hash of the data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    byte[] GetHash(Stream source, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a stream using the specified algorithm.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentException">The buffer in destination is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    int GetHash(Stream source, Span<byte> destination, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of data using the specified algorithm.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentException">The buffer in destination is too small to hold the calculated hash size.</exception>
    int GetHash(ReadOnlySpan<byte> source, Span<byte> destination, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of data using the specified algorithm.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The hash of the data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    byte[] GetHash(byte[] source, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a string using the specified algorithm.
    /// </summary>
    /// <param name="stringData">The string to hash.</param>
    /// <param name="encoding">The encoding to interpret the string</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The hash of the string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stringData"/> is <see langword="null"/>.</exception>
    byte[] GetHash(string stringData, Encoding encoding, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a string using the specified algorithm.
    /// </summary>
    /// <param name="stringData">The string to hash.</param>
    /// <param name="encoding">The encoding to interpret the string</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="InvalidOperationException">The buffer in destination is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stringData"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    int GetHash(string stringData, Encoding encoding, Span<byte> destination, HashTypeKey hashType);

    /// <summary>
    /// Computes the hash of a character span using the specified algorithm.
    /// </summary>
    /// <param name="stringData">The character span to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="encoding">The encoding to interpret the string</param>
    /// <param name="hashType">The hash type data.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="InvalidOperationException">The buffer in destination is too small to hold the calculated hash size.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stringData"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    int GetHash(ReadOnlySpan<char> stringData, Span<byte> destination, Encoding encoding, HashTypeKey hashType);

    /// <summary>
    /// Asynchronously computes the hash of a file using the specified algorithm.
    /// </summary>
    /// <param name="file">The file to hash.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The hash of the data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    ValueTask<byte[]> GetHashAsync(IFileInfo file, HashTypeKey hashType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously computes the hash of a file using the specified algorithm.
    /// </summary>
    /// <param name="file">The file to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    ValueTask<int> GetHashAsync(IFileInfo file, Memory<byte> destination, HashTypeKey hashType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously computes the hash of a stream using the specified algorithm.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The hash of the data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    ValueTask<byte[]> GetHashAsync(Stream source, HashTypeKey hashType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously computes the hash of a stream using the specified algorithm.
    /// </summary>
    /// <param name="source">The stream to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="hashType">The hash type data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
    ValueTask<int> GetHashAsync(Stream source, Memory<byte> destination, HashTypeKey hashType, CancellationToken cancellationToken = default);
}