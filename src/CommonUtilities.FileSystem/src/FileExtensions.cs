using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Validation;

namespace AnakinRaW.CommonUtilities.FileSystem;

/// <summary>
/// Provides extension methods for
/// </summary>
public static class FileExtensions
{
    
    public static void CopyWithRetry(this IFileInfo source, string destination, int retryCount = 2,
        int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => source.CopyTo(destination, true));
    }


#if !NET
    /// <inheritdoc/>
    public static bool MoveTo(this IFileInfo source, string destination, bool overwrite)
    {
        source.CopyTo(destination, overwrite);
        source.Delete();
        return true;
    }
#endif


    public static void MoveWithRetry(this IFileInfo source, string destination, bool overwrite = false, int retryCount = 2,
        int retryDelay = 500)
    {
        Requires.NotNull(source, nameof(source));
        Requires.NotNullOrEmpty(destination, nameof(destination));
        ExecuteFileSystemActionWithRetry(retryCount, retryDelay, () => source.MoveTo(destination, overwrite));
    }
    
    

    public static void DeleteIfInTemp(this IFileInfo file)
    {
        Requires.NotNull(file, nameof(file));
        if (!file.Exists || !PathExtensions.IsChildOf(FileSystem.Path.GetTempPath(), file.FullName))
            return;
        file.Delete();
    }

    public static bool DeleteWithRetry(this IFileInfo file, int retryCount = 2, int retryDelay = 500, Func<Exception, int, bool>? errorAction = null)
    {
        if (!file.Exists)
            return true;
        return ExecuteFileSystemActionWithRetry(retryCount, retryDelay, file.Delete, errorAction: (ex, attempt) =>
        {
            if (ex is UnauthorizedAccessException)
            {
                if (attempt == 0)
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        RemoveAttributes(file, FileAttributes.ReadOnly);
                        errorAction?.Invoke(ex, attempt);
                        return true;
                    }
                }
            }
            errorAction?.Invoke(ex, attempt);
            return false;
        });

    }

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
    internal static bool ExecuteFileSystemActionWithRetry(int retryCount, int retryDelay, Action fileAction,
        bool throwOnFailure = true, Func<Exception, int, bool>? errorAction = null)
    {
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