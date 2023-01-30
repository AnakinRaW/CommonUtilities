using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test;

public class StreamUtilitiesTest
{
    [Fact]
    public async Task TestStreamsNotDisposed()
    {
        var inputData = Array.Empty<byte>();
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = await StreamUtilities.CopyStreamWithProgressAsync(input, output, null, CancellationToken.None);
        Assert.Equal(0, bytesRead);
        Assert.True(output.CanWrite);
        Assert.True(output.CanRead);
    }

    [Fact]
    public async Task TestStreamLengthAndCorrectCopy()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = await StreamUtilities.CopyStreamWithProgressAsync(input, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
        output.Seek(0, SeekOrigin.Begin);
        var outputData = new byte[3];
        output.Read(outputData, 0, 3);
        Assert.Equal(inputData, outputData);
    }

    [Fact]
    public async Task TestInputLengthTooSmall()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = await StreamUtilities.CopyStreamWithProgressAsync(input, 1, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public async Task TestInputLengthTooLarge()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();
        var bytesRead = await StreamUtilities.CopyStreamWithProgressAsync(input, 4, output, null, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public async Task TestProgressReport()
    {
        var inputData = new byte[] { 1, 2, 3 };
        var input = new MemoryStream(inputData);
        var output = new MemoryStream();

        var bytesRead = 0L;
        void Action(ProgressUpdateStatus d)
        {
            bytesRead = d.BytesRead;
        }
        await StreamUtilities.CopyStreamWithProgressAsync(input, output, Action, CancellationToken.None);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    public async Task TestCancellation()
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
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await StreamUtilities.CopyStreamWithProgressAsync(input, output, Action, t.Token));
        Assert.Equal(0, bytesRead);
    }
}