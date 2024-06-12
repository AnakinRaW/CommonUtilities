using System;
using System.Text;

namespace AnakinRaW.CommonUtilities.Extensions;

/// <summary>
/// Provides PG specific extension methods to the <see cref="Encoding"/> type.
/// </summary>
public static partial class EncodingExtensions
{
    /// <summary>
    /// Encodes a string value.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The encoded string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoding"/> or <paramref name="value"/>is <see langword="null"/>.</exception>
    public static string EncodeString(this Encoding encoding, string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return encoding.EncodeString(value.AsSpan());
    }

    /// <summary>
    /// Encodes a string value.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="maxByteCount">Maximum bytes required for encoding.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The encoded string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoding"/> or <paramref name="value"/>is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="maxByteCount"/> is less than actually required.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxByteCount"/> is negative.</exception>
    public static string EncodeString(this Encoding encoding, string value, int maxByteCount)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return encoding.EncodeString(value.AsSpan(), maxByteCount);
    }

    /// <summary>
    /// Encodes a character sequence.
    /// </summary>
    /// <param name="value">The span of characters to encode.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The encoded string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
    public static string EncodeString(this Encoding encoding, ReadOnlySpan<char> value)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        return encoding.EncodeString(value, encoding.GetMaxByteCount(value.Length));
    }

    /// <summary>
    /// Encodes a character sequence.
    /// </summary>
    /// <param name="value">The span of characters to encode.</param>
    /// <param name="maxByteCount">Maximum bytes required for encoding.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The encoded string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="maxByteCount"/> is less than actually required.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxByteCount"/> is negative.</exception>
    public static unsafe string EncodeString(this Encoding encoding, ReadOnlySpan<char> value, int maxByteCount)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        if (maxByteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxByteCount), "value must not be negative.");

        var buffer = maxByteCount <= 256 ? stackalloc byte[maxByteCount] : new byte[maxByteCount];

        var stringBytes = encoding.GetBytesReadOnly(value, buffer);
        return encoding.GetString(stringBytes);
    }

    /// <summary>
    /// Encodes into a span of characters a set of characters from the specified read-only span.
    /// </summary>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="source">The span of characters to encode.</param>
    /// <param name="destination">The character span to hold the encoded characters.</param>
    /// <returns>The actual number of characters written at the span indicated by the <paramref name="destination"/> parameter.</returns>
    public static int EncodeString(this Encoding encoding, ReadOnlySpan<char> source, Span<char> destination)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        var numMaxBytes = encoding.GetMaxByteCount(source.Length);
        return EncodeString(encoding, source, destination, numMaxBytes);
    }

    /// <summary>
    /// Encodes into a span of characters a set of characters from the specified read-only span.
    /// </summary>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="source">The span of characters to encode.</param>
    /// <param name="destination">The character span to hold the encoded characters.</param>
    /// <param name="maxByteCount">Maximum bytes *not characters!* required for encoding.</param>
    /// <returns>The actual number of characters written at the span indicated by the <paramref name="destination"/> parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="maxByteCount"/> is less than actually required.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxByteCount"/> is negative.</exception>
    public static unsafe int EncodeString(this Encoding encoding, ReadOnlySpan<char> source, Span<char> destination, int maxByteCount)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        if (maxByteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxByteCount), "value must not be negative.");

        var buffer = maxByteCount > 265 ? new byte[maxByteCount] : stackalloc byte[maxByteCount];
        var bytes = encoding.GetBytesReadOnly(source, buffer);
        return encoding.GetChars(bytes, destination);
    }

    /// <summary>
    /// Encodes into a read-only span of bytes a set of characters from the specified read-only span.
    /// </summary>
    /// <remarks>
    /// The returned read-only span is sliced from <paramref name="inputBuffer"/>.
    /// This means, modifying <paramref name="inputBuffer"/> might also modify the returned read-only span. 
    /// </remarks>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="value">The span of characters to encode.</param>
    /// <param name="inputBuffer">The byte span to hold the encoded bytes.</param>
    /// <returns>The read-only byte span that holds the encoded bytes.</returns>
    public static ReadOnlySpan<byte> GetBytesReadOnly(this Encoding encoding, ReadOnlySpan<char> value, Span<byte> inputBuffer)
    {
        var pathBytesWritten = encoding.GetBytes(value, inputBuffer);
        return inputBuffer.Slice(0, pathBytesWritten);
    }
}