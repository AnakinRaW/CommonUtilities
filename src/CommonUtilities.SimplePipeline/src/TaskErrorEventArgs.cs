using System;

namespace AnakinRaW.CommonUtilities.TaskPipeline;

/// <summary>
/// <see cref="EventArgs"/> for faulted <see cref="ITask"/>.
/// </summary>
public class TaskErrorEventArgs : EventArgs
{
    private bool _cancel;

    /// <summary>
    /// The faulted Task
    /// </summary>
    public ITask Task { get; }

    /// <summary>
    /// Indicates whether the task was faulted due to cancellation. 
    /// </summary>
    /// <remarks>Once set to <see langword="true"/>, this property cannot be set to <see langword="false"/> again.</remarks>
    public bool Cancel
    {
        get => _cancel;
        set => _cancel |= value;
    }


    /// <summary>
    /// Initializes a new instance of <see cref="TaskErrorEventArgs"/>.
    /// </summary>
    /// <param name="task">The faulted task.</param>
    public TaskErrorEventArgs(ITask task)
    {
        Task = task;
    }
}