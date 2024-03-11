namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Provides access to information about the current process.
/// </summary>
public interface ICurrentProcessInfoProvider
{
    /// <summary>
    /// Gets the current process information.
    /// </summary>
    /// <returns>An <see cref="ICurrentProcessInfo"/> instance that contains information about the current process.</returns>
    public ICurrentProcessInfo GetCurrentProcessInfo();
}