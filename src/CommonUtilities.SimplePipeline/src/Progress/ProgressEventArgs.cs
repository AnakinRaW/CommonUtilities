using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Provides data for an event to report progress.
/// </summary>
/// <typeparam name="T">The type of progress information.</typeparam>
public class ProgressEventArgs<T> : EventArgs where T: new()
{
    /// <summary>
    /// Gets the text description of the current progress.
    /// </summary>
    public string ProgressText { get; }

    /// <summary>
    /// Gets the current progress value as a percentage, ranging from 0.0 to 1.0.
    /// </summary>
    public double Progress { get; }

    /// <summary>
    /// Gets the type of progress being reported.
    /// </summary>
    public ProgressType Type { get; }

    /// <summary>
    /// Gets additional detailed progress information.
    /// </summary>
    public T ProgressInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressEventArgs{T}"/> class with the specified progress text,
    /// progress value, and progress type.
    /// </summary>
    /// <param name="progressText">The text description of the current progress.</param>
    /// <param name="progress">The current progress value as a percentage, ranging from 0.0 to 1.0.</param>
    /// <param name="type">The type of progress being reported.</param>
    public ProgressEventArgs(string progressText, double progress, ProgressType type)
        : this(progressText, progress, type, new T())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressEventArgs{T}"/> class with the specified progress text,
    /// progress value, progress type, and detailed progress information.
    /// </summary>
    /// <param name="progressText">The text description of the current progress.</param>
    /// <param name="progress">The current progress value as a percentage, ranging from 0.0 to 1.0.</param>
    /// <param name="type">The type of progress being reported.</param>
    /// <param name="progressInfo">Additional detailed progress information.</param>
    public ProgressEventArgs(string progressText, double progress, ProgressType type, T progressInfo)
    {
        ThrowHelper.ThrowIfNullOrEmpty(progressText);
        ProgressText = progressText;
        Progress = progress;
        Type = type;
        ProgressInfo = progressInfo;
    }
}