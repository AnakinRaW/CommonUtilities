using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// A queue of <see cref="IStep"/>.
/// </summary>
public interface IStepQueue
{
    /// <summary>
    /// List of only those steps which are scheduled for execution of an <see cref="IRunner"/>.
    /// </summary>
    public IReadOnlyList<IStep> Steps { get; }

    /// <summary>
    /// Adds an <see cref="IStep"/> to the <see cref="IRunner"/>.
    /// </summary>
    /// <param name="activity">The step to app.</param>
    void AddStep(IStep activity);
}