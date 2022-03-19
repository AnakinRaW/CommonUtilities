using System.Threading;

namespace Sklavenwalker.CommonUtilities.TaskPipeline;

/// <summary>
/// Jobs can be planned and then run one or many operations.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Plans all included operations of this instance.
    /// </summary>
    /// <returns><see langword="true"/> if the planning was successful; <see langword="false"/> otherwise.</returns>
    bool Plan();

    /// <summary>
    /// Runs this job.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow job cancellation.</param>
    void Run(CancellationToken token = default);
}