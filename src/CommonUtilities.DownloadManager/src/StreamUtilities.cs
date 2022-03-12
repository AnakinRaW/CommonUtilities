using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace Sklavenwalker.CommonUtilities.DownloadManager;

internal static class StreamUtilities
{
    public static long CopyStream(Stream inputStream, long inputLength, Stream outputStream, CancellationToken cancellationToken)
    {
        var bufferSize = GetBufferSize(inputLength);
        inputStream.CopyToAsync(outputStream, bufferSize, cancellationToken).Wait(cancellationToken);
        return outputStream.Length;
    }

    public static long CopyStreamWithProgress(Stream inputStream, Stream outputStream,
        ProgressUpdateCallback? progress, CancellationToken cancellationToken)
    {
        return CopyStreamWithProgress(inputStream, inputStream.Length, outputStream, progress, cancellationToken);
    }

    public static long CopyStreamWithProgress(Stream inputStream, long inputLength, Stream outputStream, ProgressUpdateCallback? progress, CancellationToken cancellationToken)
    {
        if (progress == null)
            return CopyStream(inputStream, inputLength, outputStream, cancellationToken);
        
        var totalBytesRead = 0L;
        var bufferSize = GetBufferSize(inputLength);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                cancellationToken.ThrowIfCancellationRequested();
                if (bytesRead <= 0)
                    break;
                totalBytesRead += bytesRead;
                outputStream.Write(buffer, 0, bytesRead);
                if (inputLength < totalBytesRead)
                    inputLength = totalBytesRead;
                progress?.Invoke(new ProgressUpdateStatus(totalBytesRead, inputLength, 0));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return totalBytesRead;
    }

    private static int GetBufferSize(long inputLength)
    {
        return (int)Math.Max(1024, Math.Min(inputLength, 32768));
    }
}