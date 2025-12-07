#if NETSTANDARD2_0 || NETFRAMEWORK
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AnakinRaW.CommonUtilities.Extensions;

public static partial class EncodingExtensions
{
    /// <param name="encoding">The encoding to use.</param>
    extension(Encoding encoding)
    {
        /// <summary>
        /// Encodes into a span of bytes a set of characters from the specified read-only span.
        /// </summary>
        /// <param name="value">The span containing the set of characters to encode.</param>
        /// <param name="destination">The byte span to hold the encoded bytes.</param>
        /// <returns>The number of encoded bytes.</returns>
        public unsafe int GetBytes(ReadOnlySpan<char> value, Span<byte> destination)
        {
            fixed (char* charsPtr = &GetNonNullPinnableReference(value))
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(destination))
                return encoding.GetBytes(charsPtr, value.Length, bytesPtr, destination.Length);
        }

        /// <summary>
        /// Decodes all the bytes in the specified read-only byte span into a character span.
        /// </summary>
        /// <param name="bytes">A read-only span containing the sequence of bytes to decode.</param>
        /// <param name="chars">The character span receiving the decoded bytes.</param>
        /// <returns>The actual number of characters written at the span indicated by the <paramref name="chars"/> parameter.</returns>
        public unsafe int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            fixed (byte* pBytes = &GetNonNullPinnableReference(bytes))
            fixed (char* pChar = &GetNonNullPinnableReference(chars))
                return encoding.GetChars(pBytes, bytes.Length, pChar, chars.Length);
        }

        /// <summary>
        /// Decodes all the bytes in the specified byte span into a string.
        /// </summary>
        /// <param name="bytes">A read-only byte span to decode to a Unicode string.</param>
        /// <returns>A string that contains the decoded bytes from the provided read-only span.</returns>
        public unsafe string GetString(ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(bytes))
                return encoding.GetString(bytesPtr, bytes.Length);
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding the characters in the specified character span.
        /// </summary>
        /// <param name="value">The span of characters to encode.</param>
        /// <returns>The number of bytes produced by encoding the specified character span.</returns>
        public unsafe int GetByteCount(ReadOnlySpan<char> value)
        {
            fixed (char* charsPtr = &GetNonNullPinnableReference(value))
                return encoding.GetByteCount(charsPtr, value.Length);
        }
    }


    // Mimics the behavior from .NET Core where empty spans return a non-null pointer.
    private static unsafe ref T GetNonNullPinnableReference<T>(Span<T> span)
    {
        return ref span.Length != 0 ? ref MemoryMarshal.GetReference(span) : ref Unsafe.AsRef<T>((void*)1);
    }

    private static unsafe ref T GetNonNullPinnableReference<T>(ReadOnlySpan<T> span)
    {
        return ref span.Length != 0 ? ref MemoryMarshal.GetReference(span) : ref Unsafe.AsRef<T>((void*)1);
    }
}
#endif