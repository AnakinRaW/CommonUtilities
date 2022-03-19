using System;
using System.Threading;

namespace Sklavenwalker.CommonUtilities.TaskPipeline.Tasks;

/// <summary>
/// Task to Acquire a Mutex.
/// </summary>
public class AcquireMutexTask : RunnerTask
{
    private Mutex? _mutex;

    internal string MutexName { get; }

    /// <summary>
    /// Initializes a new <see cref="AcquireMutexTask"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="name">The name of the mutex.</param>
    public AcquireMutexTask(string? name, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        MutexName = name ?? Utilities.GlobalCurrentProcessMutex;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Acquiring mutex: {MutexName}";
    }

    /// <inheritdoc/>
    protected override void RunCore(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        _mutex = Utilities.CheckAndSetGlobalMutex(MutexName);
    }
    
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.Dispose(disposing);
    }
}