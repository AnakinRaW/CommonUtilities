using System;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public class ProgressEventArgs<T> : EventArgs where T: new()
{
    public string ProgressText { get; }

    public double Progress { get; }

    public ProgressType Type { get; }

    public T DetailedProgress { get; }

    public ProgressEventArgs(string progressText, double progress, ProgressType type)
        : this(progressText, progress, type, new T())
    {
    }

    public ProgressEventArgs(string progressText, double progress, ProgressType type, T detailedProgress)
    {
        Requires.NotNullOrEmpty(progressText, nameof(progressText));
        ProgressText = progressText;
        Progress = progress;
        Type = type;
        DetailedProgress = detailedProgress;
    }
}