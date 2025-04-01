using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a pipeline step that reports progress.
/// </summary>
/// <typeparam name="T">The type of the additional progress information data.</typeparam>
public interface IProgressStep<T> : IStep
{
    /// <summary>
    /// The event that is raised when the <see cref="IProgressStep{T}"/> reports progress.
    /// </summary>
    event EventHandler<ProgressEventArgs<T>> Progress;

    /// <summary>
    /// Gets the type of progress that this step reports.
    /// </summary>
    ProgressType Type { get; }

    /// <summary>
    /// Gets the total size of this step, in bytes.
    /// </summary>
    long Size { get; }
}

/// <summary>
/// Represents a pipeline step that reports progress.
/// </summary>
public interface IProgressStep : IProgressStep<object?>;