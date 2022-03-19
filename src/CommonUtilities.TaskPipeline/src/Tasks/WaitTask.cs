using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Validation;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Tasks;

/// <summary>
/// A task that waits for a given <see cref="IParallelRunner"/> to finish.
/// </summary>
public sealed class WaitTask : RunnerTask
{
    private readonly IParallelRunner _taskRunner;

    /// <summary>
    /// Initializes a new <see cref="WaitTask"/>.
    /// </summary>
    /// <param name="taskRunner">The awaitable task runner</param>
    /// <param name="serviceProvider">The service provider.</param>
    public WaitTask(IParallelRunner taskRunner, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(taskRunner, nameof(taskRunner));
        _taskRunner = taskRunner;
    }

    /// <inheritdoc/>
    public override string ToString() => "Waiting for other tasks";

    /// <summary>
    /// Waits for the instance's parallel runner.
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    /// <exception cref="StopTaskRunnerException">If awaiting the runner failed with an exception.</exception>
    protected override void RunCore(CancellationToken token)
    {
        try
        {
            _taskRunner.Wait();
        }
        catch
        {
            Logger?.LogTrace("Wait task is stopping all subsequent tasks");
            throw new StopTaskRunnerException();
        }
    }
}