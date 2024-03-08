using System;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Base class for aggregating progress from multiple steps into a single progress report.
/// </summary>
/// <typeparam name="TInfo">The type of detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TInfo> : AggregatedProgressReporter<IProgressStep, TInfo>  where TInfo : new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class with the specified progress reporter.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report progress to.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter) : base(progressReporter)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class with the specified progress reporter and step comparer.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report progress to.</param>
    /// <param name="stepComparer">The comparer to use for comparing progress steps.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEqualityComparer<IProgressStep> stepComparer) 
        : base(progressReporter, stepComparer)
    {
    }
}

/// <summary>
/// Provides an abstract class for aggregating and reporting progress for multiple <see cref="IProgressStep"/> instances.
/// </summary>
/// <typeparam name="TStep">The type of the progress step.</typeparam>
/// <typeparam name="TInfo">The type of the detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TStep, TInfo> : IStepProgressReporter
    where TStep : class, IProgressStep where TInfo : new()
{
    /// <summary>
    /// Gets the progress type this instance reports to.
    /// </summary>
    protected abstract ProgressType Type { get; }

    private readonly IProgressReporter<TInfo> _progressReporter;

    private readonly HashSet<TStep> _progressSteps;

    /// <summary>
    /// Gets the total size of all steps.
    /// </summary>
    protected internal long TotalSize { get; private set; }

    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    protected int TotalStepCount => _progressSteps.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TStep, TInfo}"/> class
    /// with the specified <see cref="IProgressReporter{TInfo}"/> instance.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter) : 
        this(progressReporter, EqualityComparer<TStep>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TStep, TInfo}"/> class
    /// with the specified <see cref="IProgressReporter{TInfo}"/> instance and <see cref="IEqualityComparer{TStep}"/> instance.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress.</param>
    /// <param name="stepComparer">The comparer to use for comparing steps.</param>
    protected AggregatedProgressReporter(
        IProgressReporter<TInfo> progressReporter,
        IEqualityComparer<TStep> stepComparer)
    {
        _progressSteps = new HashSet<TStep>(stepComparer);
        _progressReporter = progressReporter;
    }

    /// <summary>
    /// Initializes this instance with the specified steps.
    /// </summary>
    /// <param name="progressSteps">The steps associated to this instance.</param>
    public void Initialize(IEnumerable<TStep> progressSteps)
    {
        foreach (var task in progressSteps)
        {
            _progressSteps.Add(task);
            TotalSize += task.Size;
        }
    }

    /// <summary>
    /// Reports the progress of the specified progress step.
    /// </summary>
    /// <param name="step">The step to report the progress for.</param>
    /// <param name="progress">The progress value.</param>
    public void Report(TStep step, double progress)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));
        if (!_progressSteps.Contains(step))
            return;

        ReportInternal(step, progress);
    }

    void IStepProgressReporter.Report(IProgressStep step, double progress)
    {
        if (step == null) 
            throw new ArgumentNullException(nameof(step));

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

    /// <summary>
    /// Gets the progress text to report for the given step.
    /// </summary>
    /// <param name="step">The step for which to get the progress text.</param>
    /// <returns>The progress text to report for the given step.</returns>
    protected abstract string GetProgressText(TStep step);

    /// <summary>
    /// Calculates the aggregated progress for the given step and progress value, and updates the progress info
    /// object with any additional information to be included in the progress report.
    /// </summary>
    /// <param name="task">The step for which to calculate the progress.</param>
    /// <param name="progress">The progress value for the step.</param>
    /// <param name="progressInfo">The object to update with additional progress information.</param>
    /// <returns>The aggregated progress value for all steps.</returns>
    protected abstract double CalculateAggregatedProgress(TStep task, double progress, ref TInfo progressInfo);
}