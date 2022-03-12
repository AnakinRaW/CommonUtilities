using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Test;

public class StreamUtilitiesTest
{
    [Fact]
    public void TestStreamsNotDisposed()
    {
        var inputData = Array.Empty<byte>();
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = StreamUtilities.CopyStreamWithProgress(input, output, null, CancellationToken.None);
        Assert.Equal(0, bytesRead);
        Assert.True(output.CanWrite);
        Assert.True(output.CanRead);
    }

    [Fact]
    public void TestStreamLengthAndCorrectCopy()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = StreamUtilities.CopyStreamWithProgress(input, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
        output.Seek(0, SeekOrigin.Begin);
        var outputData = new byte[3];
        output.Read(outputData, 0, 3);
        Assert.Equal(inputData, outputData);
    }

    [Fact]
    public void TestInputLengthTooSmall()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = StreamUtilities.CopyStreamWithProgress(input, 1, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public void TestInputLengthTooLarge()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = StreamUtilities.CopyStreamWithProgress(input, 4, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public void TestProgressReport()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();

        var bytesRead = 0L;
        void Action(ProgressUpdateStatus d)
        {
            bytesRead = d.BytesRead;
        }
        StreamUtilities.CopyStreamWithProgress(input, output, Action, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public void TestCancellation()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();

        var t = new CancellationTokenSource();
        var bytesRead = 0L;
        void Action(ProgressUpdateStatus d)
        {
            bytesRead = d.BytesRead;
        }
        t.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            StreamUtilities.CopyStreamWithProgress(input, output, Action, t.Token));
        Assert.Equal(0, bytesRead);
    }
}