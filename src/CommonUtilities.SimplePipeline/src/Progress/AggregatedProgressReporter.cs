using System;
using System.Collections.Generic;
using System.Linq;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

public abstract class AggregatedProgressReporter<TInfo> : AggregatedProgressReporter<IProgressStep, TInfo>  where TInfo : new()
{
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter) : base(progressReporter)
    {
    }

    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEqualityComparer<IProgressStep> stepComparer) 
        : base(progressReporter, stepComparer)
    {
    }
}

public abstract class AggregatedProgressReporter<TStep, TInfo> : IStepProgressReporter
    where TStep : class, IProgressStep where TInfo : new()
{
    protected abstract ProgressType Type { get; }

    private readonly IProgressReporter<TInfo> _progressReporter;

    private readonly HashSet<TStep> _progressSteps;

    protected long TotalSize { get; private set; }

    protected int TotalStepCount => _progressSteps.Count;

    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter) : 
        this(progressReporter, EqualityComparer<TStep>.Default)
    {
    }

    protected AggregatedProgressReporter(
        IProgressReporter<TInfo> progressReporter,
        IEqualityComparer<TStep> stepComparer)
    {
        _progressSteps = new HashSet<TStep>(stepComparer);
        _progressReporter = progressReporter;
    }

    public void Initialize(IEnumerable<TStep> progressSteps)
    {
        foreach (var task in progressSteps)
        {
            _progressSteps.Add(task);
            TotalSize += task.Size;
        }
    }

    public void Report(TStep step, double progress)
    {
        Requires.NotNull(step, nameof(step));

        if (!_progressSteps.Contains(step))
            return;

        ReportInternal(step, progress);
    }

    void IStepProgressReporter.Report(IProgressStep step, double progress)
    {
        Requires.NotNull(step, nameof(step));

        if (!_progressSteps.Contains(step))
            return;

        if (step is not TStep tStep)
            throw new InvalidCastException($"Cannot cast step '{step.GetType().FullName}' to {typeof(TStep).FullName}");

        ReportInternal(tStep, progress);
    }

    private void ReportInternal(TStep step, double progress)
    {
        var actualProgressInfo = new TInfo();
        var currentProgress = 0.0;

        if (TotalSize > 0)
            currentProgress = CalculateAggregatedProgress(step, progress, ref actualProgressInfo);

        _progressReporter.Report(GetProgressText(step), currentProgress, Type, actualProgressInfo);
    }

    protected abstract string GetProgressText(TStep step);

    protected abstract double CalculateAggregatedProgress(TStep task, double progress, ref TInfo progressInfo);
}