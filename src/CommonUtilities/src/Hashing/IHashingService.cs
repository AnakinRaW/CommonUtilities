using System;
using System.IO;
using System.IO.Abstractions;
#if NET
using System.Threading.Tasks;
#endif


namespace AnakinRaW.CommonUtilities.Hashing;

/// <summary>
/// Service for calculating hash codes.
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Calculates a hash code of a given file.
    /// </summary>
    /// <param name="file">The file to get the hash code for.</param>
    /// <param name="hashType">The hash algorithm.</param>
    /// <returns>The hash code of the file.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="NotSupportedException">If no hashing algorithm implementation could be found.</exception>
    /// <exception cref="InvalidOperationException">If the file cannot be read.</exception>
    byte[] GetFileHash(IFileInfo file, HashType hashType);

    /// <summary>
    /// Calculates a hash code of a given data stream by reading it from start to end.
    /// </summary>
    /// <param name="stream">The target stream</param>
    /// <param name="hashType">The hash algorithm.</param>
    /// <returns>The hash code of the stream</returns>
    /// <exception cref="InvalidOperationException">If the file cannot be read.</exception>
    /// <exception cref="NotSupportedException">If no hashing algorithm implementation could be found.</exception>
    byte[] GetStreamHash(Stream stream, HashType hashType);


#if NET
    /// <summary>
    /// Calculates a hash code of a given file asynchronously.
    /// </summary>
    /// <param name="file">The file to get the hash code for.</param>
    /// <param name="hashType">The hash algorithm</param>
    /// <returns>The hash code of the file.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">If the file cannot be read.</exception>
    /// <exception cref="NotSupportedException">If no hashing algorithm implementation could be found.</exception>
    Task<byte[]> HashFileAsync(IFileInfo file, HashType hashType);
#endif
}