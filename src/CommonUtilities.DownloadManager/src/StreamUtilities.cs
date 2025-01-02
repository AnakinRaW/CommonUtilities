using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.DownloadManager;

internal static class StreamUtilities
{
    public static async Task<long> CopyStream(Stream inputStream, long inputLength, Stream outputStream, CancellationToken cancellationToken)
    {
        var bufferSize = GetBufferSize(inputLength);
        await inputStream.CopyToAsync(outputStream, bufferSize, cancellationToken);
        return outputStream.Length;
    }

    public static Task<long> CopyStreamWithProgressAsync(Stream inputStream, Stream outputStream,
        DownloadUpdateCallback? progress, CancellationToken cancellationToken)
    {
        return CopyStreamWithProgressAsync(inputStream, inputStream.Length, outputStream, progress, cancellationToken);
    }

    public static async Task<long> CopyStreamWithProgressAsync(Stream inputStream, long inputLength, Stream outputStream, DownloadUpdateCallback? progress, CancellationToken cancellationToken)
    {
        if (progress == null)
            return await CopyStream(inputStream, inputLength, outputStream, cancellationToken);
        
        var totalBytesRead = 0L;
        var bufferSize = GetBufferSize(inputLength);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            while (true)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                    break;
                totalBytesRead += bytesRead;
                await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                if (inputLength < totalBytesRead)
                    inputLength = totalBytesRead;
                progress?.Invoke(new DownloadUpdate(totalBytesRead, inputLength, 0));
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