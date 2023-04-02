using System.Collections.Generic;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public abstract class AggregatedComponentProgressReporter<T> : IStepProgressReporter where T : new()
{
    protected abstract ProgressType Type { get; }

    private readonly IProgressReporter<T> _progressReporter;

    private readonly HashSet<IProgressStep> _componentProgressCollection;

    protected long TotalSize { get; private set; }

    protected int TotalComponentCount => _componentProgressCollection.Count;

    protected AggregatedComponentProgressReporter(IProgressReporter<T> progressReporter) :
        this(progressReporter, EqualityComparer<IProgressStep>.Default)
    {
    }

    protected AggregatedComponentProgressReporter(
        IProgressReporter<T> progressReporter,
        IEqualityComparer<IProgressStep> stepComparer)
    {
        _componentProgressCollection = new HashSet<IProgressStep>(stepComparer);
        _progressReporter = progressReporter;
    }

    internal void Initialize(IEnumerable<IProgressStep> progressSteps)
    {
        foreach (var task in progressSteps)
        {
            _componentProgressCollection.Add(task);
            TotalSize += task.Size;
        }
    }

    public void Report(IProgressStep step, double progress)
    {
        Requires.NotNull(step, nameof(step));

        if (!_componentProgressCollection.Contains(step))
            return;

        var actualProgressInfo = new T();
        var currentProgress = 0.0;

        if (TotalSize > 0)
            currentProgress = CalculateAggregatedProgress(step, progress, ref actualProgressInfo);

        _progressReporter.Report(GetProgressText(step), currentProgress, Type, actualProgressInfo);
    }

    protected abstract string GetProgressText(IProgressStep step);

    protected abstract double CalculateAggregatedProgress(IProgressStep task, double progress, ref T progressInfo);
}