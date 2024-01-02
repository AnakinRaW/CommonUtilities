namespace AnakinRaW.CommonUtilities;

/// <inheritdoc/>
public sealed class CurrentProcessInfoProvider : ICurrentProcessInfoProvider
{
    /// <inheritdoc/>
    public ICurrentProcessInfo GetCurrentProcessInfo()
    {
        return CurrentProcessInfo.Current;
    }
}