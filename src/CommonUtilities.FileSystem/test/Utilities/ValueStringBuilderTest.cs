using AnakinRaW.CommonUtilities.FileSystem.Utilities;
using System;
using System.Text;
using Xunit;

namespace AnakinRaW.CommonUtilities.FileSystem.Test.Utilities;

// Copied from https://github.com/dotnet/runtime
public class ValueStringBuilderTests
{
    [Fact]
    public void Ctor_Default_CanAppend()
    {
        var vsb = default(ValueStringBuilder);
        Assert.Equal(0, vsb.Length);

        vsb.Append('a');
        Assert.Equal(1, vsb.Length);
        Assert.Equal("a", vsb.ToString());
    }

    [Fact]
    public void Ctor_Span_CanAppend()
    {
        var vsb = new ValueStringBuilder(new char[1]);
        Assert.Equal(0, vsb.Length);

        vsb.Append('a');
        Assert.Equal(1, vsb.Length);
        Assert.Equal("a", vsb.ToString());
    }

    [Fact]
    public void Ctor_InitialCapacity_CanAppend()
    {
        var vsb = new ValueStringBuilder(1);
        Assert.Equal(0, vsb.Length);

        vsb.Append('a');
        Assert.Equal(1, vsb.Length);
        Assert.Equal("a", vsb.ToString());
    }

    [Fact]
    public void Append_Char_MatchesStringBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();
        for (var i = 1; i <= 100; i++)
        {
            sb.Append((char)i);
            vsb.Append((char)i);
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void Append_String_MatchesStringBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();
        for (var i = 1; i <= 100; i++)
        {
            var s = i.ToString();
            sb.Append(s);
            vsb.Append(s);
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Theory]
    [InlineData(0, 4 * 1024 * 1024)]
    [InlineData(1025, 4 * 1024 * 1024)]
    [InlineData(3 * 1024 * 1024, 6 * 1024 * 1024)]
    public void Append_String_Large_MatchesStringBuilder(int initialLength, int stringLength)
    {
        var sb = new StringBuilder(initialLength);
        var vsb = new ValueStringBuilder(new char[initialLength]);

        var s = new string('a', stringLength);
        sb.Append(s);
        vsb.Append(s);

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void Append_CharInt_MatchesStringBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();
        for (var i = 1; i <= 100; i++)
        {
            sb.Append((char)i, i);
            vsb.Append((char)i, i);
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public unsafe void Append_PtrInt_MatchesStringBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();
        for (var i = 1; i <= 100; i++)
        {
            var s = i.ToString();
            fixed (char* p = s)
            {
                sb.Append(p, s.Length);
                vsb.Append(p, s.Length);
            }
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void AppendSpan_DataAppendedCorrectly()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();

        for (var i = 1; i <= 1000; i++)
        {
            var s = i.ToString();

            sb.Append(s);

            var span = vsb.AppendSpan(s.Length);
            Assert.Equal(sb.Length, vsb.Length);

            s.AsSpan().CopyTo(span);
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void Insert_IntCharInt_MatchesStringBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();
        var rand = new Random(42);

        for (var i = 1; i <= 100; i++)
        {
            var index = rand.Next(sb.Length);
            sb.Insert(index, new string((char)i, 1), i);
            vsb.Insert(index, (char)i, i);
        }

        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void AsSpan_ReturnsCorrectValue_DoesntClearBuilder()
    {
        var sb = new StringBuilder();
        var vsb = new ValueStringBuilder();

        for (var i = 1; i <= 100; i++)
        {
            var s = i.ToString();
            sb.Append(s);
            vsb.Append(s);
        }

        var resultString = vsb.AsSpan().ToString();
        Assert.Equal(sb.ToString(), resultString);

        Assert.NotEqual(0, sb.Length);
        Assert.Equal(sb.Length, vsb.Length);
        Assert.Equal(sb.ToString(), vsb.ToString());
    }

    [Fact]
    public void ToString_ClearsBuilder_ThenReusable()
    {
        const string text1 = "test";
        var vsb = new ValueStringBuilder();

        vsb.Append(text1);
        Assert.Equal(text1.Length, vsb.Length);

        var s = vsb.ToString();
        Assert.Equal(text1, s);

        Assert.Equal(0, vsb.Length);
        Assert.Equal(string.Empty, vsb.ToString());
        Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

        const string text2 = "another test";
        vsb.Append(text2);
        Assert.Equal(text2.Length, vsb.Length);
        Assert.Equal(text2, vsb.ToString());
    }

    [Fact]
    public void TryCopyTo_FailsWhenDestinationIsTooSmall_SucceedsWhenItsLargeEnough()
    {
        var vsb = new ValueStringBuilder();

        const string text = "expected text";
        vsb.Append(text);
        Assert.Equal(text.Length, vsb.Length);

        Span<char> dst = new char[text.Length - 1];
        Assert.False(vsb.TryCopyTo(dst, out var charsWritten));
        Assert.Equal(0, charsWritten);
        Assert.Equal(0, vsb.Length);
    }

    [Fact]
    public void TryCopyTo_ClearsBuilder_ThenReusable()
    {
        const string text1 = "test";
        var vsb = new ValueStringBuilder();

        vsb.Append(text1);
        Assert.Equal(text1.Length, vsb.Length);

        Span<char> dst = new char[text1.Length];
        Assert.True(vsb.TryCopyTo(dst, out var charsWritten));
        Assert.Equal(text1.Length, charsWritten);
        Assert.Equal(text1, dst.ToString());

        Assert.Equal(0, vsb.Length);
        Assert.Equal(string.Empty, vsb.ToString());
        Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

        const string text2 = "another test";
        vsb.Append(text2);
        Assert.Equal(text2.Length, vsb.Length);
        Assert.Equal(text2, vsb.ToString());
    }

    [Fact]
    public void Dispose_ClearsBuilder_ThenReusable()
    {
        const string text1 = "test";
        var vsb = new ValueStringBuilder();

        vsb.Append(text1);
        Assert.Equal(text1.Length, vsb.Length);

        vsb.Dispose();

        Assert.Equal(0, vsb.Length);
        Assert.Equal(string.Empty, vsb.ToString());
        Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

        const string text2 = "another test";
        vsb.Append(text2);
        Assert.Equal(text2.Length, vsb.Length);
        Assert.Equal(text2, vsb.ToString());
    }

    [Fact]
    public void Indexer()
    {
        const string text1 = "foobar";
        var vsb = new ValueStringBuilder();

        vsb.Append(text1);

        Assert.Equal('b', vsb[3]);
        vsb[3] = 'c';
        Assert.Equal('c', vsb[3]);
        vsb.Dispose();
    }

    [Fact]
    public void EnsureCapacity_IfRequestedCapacityWins()
    {
        // Note: constants used here may be dependent on minimal buffer size
        // the ArrayPool is able to return.
        var builder = new ValueStringBuilder(stackalloc char[32]);

        builder.EnsureCapacity(65);

        Assert.Equal(128, builder.Capacity);
    }

    [Fact]
    public void EnsureCapacity_IfBufferTimesTwoWins()
    {
        var builder = new ValueStringBuilder(stackalloc char[32]);

        builder.EnsureCapacity(33);

        Assert.Equal(64, builder.Capacity);
        builder.Dispose();
    }

    [Fact]
    public void EnsureCapacity_NoAllocIfNotNeeded()
    {
        // Note: constants used here may be dependent on minimal buffer size
        // the ArrayPool is able to return.
        var builder = new ValueStringBuilder(stackalloc char[64]);

        builder.EnsureCapacity(16);

        Assert.Equal(64, builder.Capacity);
        builder.Dispose();
    }
}