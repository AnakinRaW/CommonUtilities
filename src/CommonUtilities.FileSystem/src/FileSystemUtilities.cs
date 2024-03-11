using System;
using System.IO;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides helper methods to work with the file system-
/// </summary>
public static class FileSystemUtilities
{
    /// <summary>
    /// Tries to execute a given IO action. In the event of an <see cref="IOException"/>
    /// or <see cref="UnauthorizedAccessException"/> the action will be tried again.
    /// </summary>
    /// <param name="retryCount">Amount of retries of <paramref name="fileAction"/></param>
    /// <param name="retryDelay">The delay in ms between each retry.</param>
    /// <param name="fileAction">The action the get performed.</param>
    /// <param name="throwOnFailure">When set to <see langword="true"/>, if all retries are unsuccessful the causing exception will be thrown.</param>
    /// <param name="errorAction">Callback which gets invoked if an <see cref="IOException"/>
    /// or <see cref="UnauthorizedAccessException"/> is was thrown during the <paramref name="fileAction"/> execution..</param>
    /// <returns><see langword="true"/>if the operation was successful. <see langword="false"/> otherwise.</returns>
    public static bool ExecuteFileSystemActionWithRetry(int retryCount, int retryDelay, Action fileAction,
        bool throwOnFailure = true, Func<Exception, int, bool>? errorAction = null)
    {
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "retryCount must be positive");

        if (fileAction is null)
            throw new ArgumentNullException(nameof(fileAction));

        var num = retryCount + 1;
        for (var index = 0; index < num; ++index)
        {
            try
            {
                fileAction();
                return true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                if (!throwOnFailure || index + 1 < num)
                {
                    if (errorAction != null)
                    {
                        if (!errorAction(ex, index))
                        {
                            if (index + 1 >= num)
                                continue;
                        }
                        else
                            continue;
                    }

                    Task.Delay(retryDelay).Wait();
                }
                else
                    throw;
            }
        }
        return false;
    }
}