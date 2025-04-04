using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Progress;

/// <summary>
/// Base class for aggregating progress from multiple steps into a single progress report.
/// </summary>
/// <typeparam name="TInfo">The type of detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TInfo> : AggregatedProgressReporter<IProgressStep<TInfo>, TInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class with the specified progress reporter.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progressReporter"/> or <paramref name="steps"/> is <see langword="null"/>.</exception>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEnumerable<IProgressStep<TInfo>> steps) : base(progressReporter, steps)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TInfo}"/> class
    /// with the specified progress reporter and steps.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    /// <param name="equalityComparer">The equality comparer used to identify whether a reporting step is contained in this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progressReporter"/> or <paramref name="steps"/> or <paramref name="equalityComparer"/> is <see langword="null"/>.</exception>
    protected AggregatedProgressReporter(
        IProgressReporter<TInfo> progressReporter, 
        IEnumerable<IProgressStep<TInfo>> steps,
        IEqualityComparer<IProgressStep<TInfo>> equalityComparer) : base(progressReporter, steps, equalityComparer)
    {
    }
}

/// <summary>
/// Base class for aggregating progress from multiple steps into a single progress report.
/// </summary>
/// <typeparam name="TStep">The type of the progress step.</typeparam>
/// <typeparam name="TInfo">The type of the detailed progress information.</typeparam>
public abstract class AggregatedProgressReporter<TStep, TInfo> : DisposableObject
    where TStep : IProgressStep<TInfo>
{
    private readonly IProgressReporter<TInfo> _progressReporter;
    private readonly HashSet<TStep> _progressSteps;

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
    /// <exception cref="ArgumentNullException"><paramref name="progressReporter"/> or <paramref name="steps"/> is <see langword="null"/>.</exception>
    protected AggregatedProgressReporter(IProgressReporter<TInfo> progressReporter, IEnumerable<TStep> steps) 
        : this(progressReporter, steps, EqualityComparer<TStep>.Default)
    {
    }

    /// <summary>
    /// Gets the progress text to report for the given step.
    /// </summary>
    /// <param name="step">The step for which to get the progress text.</param>
    /// <param name="progressText">The progress text of the original progress event.</param>
    /// <returns>The progress text to report for the given step.</returns>
    protected abstract string? GetProgressText(TStep step, string? progressText);

    /// <summary>
    /// Calculates the aggregated progress for the given step and progress value, and updates the progress info
    /// object with any additional information to be included in the progress report.
    /// </summary>
    /// <param name="step">The step for which to calculate the progress.</param>
    /// <param name="progress">The progress value for the step.</param>
    /// <returns>The aggregated progress for all steps.</returns>
    protected abstract ProgressEventArgs<TInfo> CalculateAggregatedProgress(TStep step, ProgressEventArgs<TInfo> progress);

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatedProgressReporter{TStep, TInfo}"/> class
    /// with the specified progress reporter and steps.
    /// </summary>
    /// <param name="progressReporter">The progress reporter to report the aggregated progress to.</param>
    /// <param name="steps">The steps that can report progress to this instance.</param>
    /// <param name="equalityComparer">The equality comparer used to identify whether a reporting step is contained in this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progressReporter"/> or <paramref name="steps"/> or <paramref name="equalityComparer"/> is <see langword="null"/>.</exception>
    protected AggregatedProgressReporter(
        IProgressReporter<TInfo> progressReporter, 
        IEnumerable<TStep> steps, 
        IEqualityComparer<TStep> equalityComparer)
    {
        if (steps == null)
            throw new ArgumentNullException(nameof(steps));
        if (equalityComparer == null) 
            throw new ArgumentNullException(nameof(equalityComparer));
       
        _progressSteps = new HashSet<TStep>(equalityComparer);
        _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));

        foreach (var step in steps)
        {
            if (!_progressSteps.Add(step))
                continue;
            step.Progress += OnStepProgress;
            TotalSize += step.Size;
        }
    }

    private void OnStepProgress(object sender, ProgressEventArgs<TInfo> e)
    {
        if (sender is not TStep step)
            throw new InvalidCastException($"Cannot cast '{sender.GetType()}' to {typeof(TStep)}");
        if (!_progressSteps.Contains(step))
            return;

        var aggregatedProgress = CalculateAggregatedProgress(step, e);
        _progressReporter.Report(
            aggregatedProgress.Progress, 
            GetProgressText(step, aggregatedProgress.ProgressText), 
            step.Type, 
            aggregatedProgress.ProgressInfo);
    }

    /// <inheritdoc />
    protected override void DisposeResources()
    {
        foreach (var step in _progressSteps) 
            step.Progress -= OnStepProgress;
        _progressSteps.Clear();
    }
}