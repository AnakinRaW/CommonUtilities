using System;
using System.Linq;
using System.Text;
using AnakinRaW.CommonUtilities.Extensions;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test;

public class EncodingExtensionsTest
{
    private void ForEachEncoding(Action<Encoding> action)
    {
        var encodings = Encoding.GetEncodings().Select(x => Encoding.GetEncoding(x.Name));
        foreach (var encoding in encodings)
        {
            try
            {
                action(encoding);
            }
            catch
            {
                Console.WriteLine($"Failed for encoding '{encoding}'");
                throw;
            }
        }
    }


    #region .NET Framework Extensions

    // Enabled for .NET Core too, to make sure both runtimes behave the same.

    [Theory]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("  ")]
    [InlineData("123")]
    [InlineData("üöä")]
    [InlineData("123ü")]
    [InlineData("😅")]
    public void Test_GetBytes(string data)
    {
        ForEachEncoding(encoding =>
        {
            var expectedBytes = encoding.GetBytes(data);
            var actualBytes = new byte[encoding.GetMaxByteCount(data.Length)].AsSpan();
            var n = encoding.GetBytes(data.AsSpan(), actualBytes);
            Assert.Equal(expectedBytes, actualBytes.Slice(0, n).ToArray());
        });
    }

    [Fact]
    public void Test_GetBytes_DefaultSpan()
    {
        ForEachEncoding(e =>
        {
            var bytes = new byte[] { 1, 2 };
            var n = e.GetBytes(default, bytes);

            Assert.Equal(0, n);
            // Check that bytes is unaltered
            Assert.Equal(new byte[] { 1, 2 }, bytes);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("  ")]
    [InlineData("123")]
    [InlineData("üöä")]
    [InlineData("123ü")]
    [InlineData("😅")]
    public void Test_GetByteCount(string data)
    {
        ForEachEncoding(e =>
        {
            var actualCount = e.GetByteCount(data);
            Assert.Equal(actualCount, e.GetByteCount(data.AsSpan()));
        });
    }

    [Fact]
    public void Test_GetByteCount_DefaultSpan()
    {
        ForEachEncoding(e =>
        {
            Assert.Equal(0, e.GetByteCount(default(ReadOnlySpan<char>)));
        });
    }

#if NETFRAMEWORK

    [Fact]
    public void Test_GetString_DefaultSpan()
    {
        ForEachEncoding(e =>
        {
            // Force compiler to use EncodingExtensions instead of implicit casting to byte[]
            Assert.Equal(string.Empty, EncodingExtensions.GetString(e, default(ReadOnlySpan<byte>)));
        });
    }

    [Fact]
    public void Test_GetString()
    {
        // Force compiler to use EncodingExtensions instead of implicit casting to byte[]
        Assert.Equal("\0", EncodingExtensions.GetString(Encoding.ASCII, new byte[] { 00 }.AsSpan()));
        Assert.Equal("012", EncodingExtensions.GetString(Encoding.ASCII, "012"u8.ToArray().AsSpan()));
        Assert.Equal("?", EncodingExtensions.GetString(Encoding.ASCII, new byte[] { 255 }.AsSpan()));
    }
#endif


    #endregion

    #region EncodeString

    [Fact]
    public void Test_EncodeString_NullArgs_Throws()
    {
        Encoding encoding = null!;
        Assert.Throws<ArgumentNullException>(() => encoding.EncodeString(""));
        Assert.Throws<ArgumentNullException>(() => encoding.EncodeString("", 0));

        ForEachEncoding(e =>
        {
            Assert.Throws<ArgumentNullException>(() => e.EncodeString((string)null!));
            Assert.Throws<ArgumentNullException>(() => e.EncodeString((string)null!, 0));
        });
    }

    [Fact]
    public void Test_EncodeString_NegativeCount_Throws()
    {
        ForEachEncoding(e =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => e.EncodeString("123", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.EncodeString("123", int.MinValue));

        });
    }

    [Fact]
    public void Test_EncodeString_DefaultSpan()
    {
        var encodings = Encoding.GetEncodings().Select(x => Encoding.GetEncoding(x.Name));

        foreach (var encoding in encodings)
        {
            Assert.Equal(string.Empty, encoding.EncodeString(default(ReadOnlySpan<char>)));
            Assert.Equal(string.Empty, encoding.EncodeString((default(ReadOnlySpan<char>)), 5));
        }
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("\0", "\0")]
    [InlineData("  ", "  ")]
    [InlineData("123", "123")]
    [InlineData("üöä", "???")]
    [InlineData("123ü", "123?")]
    [InlineData("😅", "??")]
    public void Test_EncodeString_EncodeASCII(string input, string expected)
    {
        var encoding = Encoding.ASCII;
        var result = encoding.EncodeString(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("\0", "\0")]
    [InlineData("  ", "  ")]
    [InlineData("123", "123")]
    [InlineData("üöä", "üöä")]
    [InlineData("123ü", "123ü")]
    [InlineData("😅", "😅")]
    public void Test_EncodeString_EncodeUnicode(string input, string expected)
    {
        var encoding = Encoding.Unicode;
        var result = encoding.EncodeString(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123", "123", 3)]
    [InlineData("123", "123", 4)]
    [InlineData("", "", 0)]
    [InlineData("", "", 1)]
    public void Test_EncodeString_Encode_CustomCount(string input, string expected, int count)
    {
        var encoding = Encoding.ASCII;
        var result = encoding.EncodeString(input, count);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", 0)]
    [InlineData("123", 2)]
    public void Test_EncodeString_Encode_CustomCountInvalid_Throws(string input, int count)
    {
        var encoding = Encoding.ASCII;
        Assert.ThrowsAny<ArgumentException>(() => encoding.EncodeString(input, count));
    }

    [Fact]
    public void Test_EncodeString_Encode_LongString()
    {
        var encoding = Encoding.ASCII;
        var result = encoding.EncodeString(new string('a', 512));
        Assert.Equal(new string('a', 512), result);
    }

    [Fact]
    public void Test_EncodeString_Encode_CountError_Throws()
    {
        ForEachEncoding(e =>
        {
            var actualCount = e.GetByteCount("123");
            Assert.Throws<ArgumentException>(() => e.EncodeString("123", actualCount - 1));
        });
    }

    #endregion

    #region GetBytesReadOnly

    [Fact]
    public void Test_GetBytesReadOnly_DefaultSpan()
    {
        ForEachEncoding(e =>
        {
            var bufferBytes = new byte[] { 1, 2 };

            var bytes = e.GetBytesReadOnly(default, bufferBytes);

            Assert.True(bytes.IsEmpty);
            // Check that bytes is unaltered
            Assert.Equal(new byte[] { 1, 2 }, bufferBytes);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("  ")]
    [InlineData("123")]
    [InlineData("üöä")]
    [InlineData("123ü")]
    [InlineData("😅")]
    public void Test_GetBytesReadOnly(string? input)
    {
        ForEachEncoding(e =>
        {
            var expectedBytes = e.GetBytes(input);

            var maxByteCount = input is null ? 5 : e.GetByteCount(input) + 5;
            var buffer = new byte[maxByteCount].AsSpan();
            var stringBytes = e.GetBytesReadOnly(input.AsSpan(), buffer);
            Assert.Equal(expectedBytes, stringBytes.ToArray());
        });
    }

    [Fact]
    public void Test_GetBytesReadOnly_BufferMutationChangesResult()
    {
        var encoding = Encoding.ASCII;
        Span<byte> buffer = stackalloc byte[10];
        var roSpan = encoding.GetBytesReadOnly("123".AsSpan(), buffer);

        Assert.Equal("123"u8.ToArray(), roSpan.ToArray());

        buffer.Clear();

        Assert.True(roSpan.ToArray().All(x => x == 0));
    }

    #endregion
}