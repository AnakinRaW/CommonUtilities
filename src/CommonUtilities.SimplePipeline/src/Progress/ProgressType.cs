namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public abstract record ProgressType
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }
}