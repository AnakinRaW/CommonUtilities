using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Base class for aggregating progress from multiple steps into a single progress report.
/// </summary>
/// <typeparam name="TInfo">The type of detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TInfo> : AggregatedProgressReporter<IProgressStep, TInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class with the specified progress reporter.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEnumerable<IProgressStep> steps) : base(progressReporter, steps)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class
    /// with the specified progress reporter and steps.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    /// <param name="equalityComparer">The equality comparer used to identify whether a reporting step is contained in this instance.</param>
    protected AggregatedProgressReporter(
        IProgressReporter<TInfo> progressReporter, 
        IEnumerable<IProgressStep> steps,
        IEqualityComparer<IProgressStep> equalityComparer) : base(progressReporter, steps, equalityComparer)
    {
    }
}

/// <summary>
/// Base class for a <see cref="IStepProgressReporter"/> that supports reporting aggregated progress information for multiple progress steps.
/// </summary>
/// <typeparam name="TStep">The type of the progress step.</typeparam>
/// <typeparam name="TInfo">The type of the detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TStep, TInfo> : IStepProgressReporter
    where TStep : IProgressStep
{
    private readonly IProgressReporter<TInfo> _progressReporter;
    private readonly HashSet<TStep> _progressSteps;

    /// <summary>
    /// Gets the progress type this instance reports to.
    /// </summary>
    protected abstract ProgressType Type { get; }

    /// <summary>
    /// Gets the total size of all steps.
    /// </summary>
    protected internal long TotalSize { get; }

    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    protected internal int TotalStepCount => _progressSteps.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TStep, TInfo}"/> class
    /// with the specified progress reporter and steps.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEnumerable<TStep> steps) 
        : this(progressReporter, steps, EqualityComparer<TStep>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TStep, TInfo}"/> class
    /// with the specified progress reporter and steps.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    /// <param name="equalityComparer">The equality comparer used to identify whether a reporting step is contained in this instance.</param>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEnumerable<TStep> steps, IEqualityComparer<TStep> equalityComparer)
    {
        if (steps == null)
            throw new ArgumentNullException(nameof(steps));
        if (equalityComparer == null) 
            throw new ArgumentNullException(nameof(equalityComparer));
       
        _progressSteps = new HashSet<TStep>(equalityComparer);
        _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));

        foreach (var task in steps)
        {
            if (_progressSteps.Add(task))
                TotalSize += task.Size;
        }
    }

    /// <summary>
    /// Reports the progress of the specified progress step if registered for the progress reporter.
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
        if (step is not TStep tStep)
            throw new InvalidCastException($"Cannot cast step '{step.GetType().FullName}' to {typeof(TStep).FullName}");
        Report(tStep, progress);
    }

    private void ReportInternal(TStep step, double progress)
    {
        var currentProgress = CalculateAggregatedProgress(step, progress, out var actualProgressInfo);
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
    protected abstract double CalculateAggregatedProgress(TStep task, double progress, out TInfo progressInfo);
}