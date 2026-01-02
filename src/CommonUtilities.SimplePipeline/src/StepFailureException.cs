using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a set of errors during pipeline execution with one or multiple steps.
/// </summary>
public sealed class StepFailureException : Exception
{
    /// <summary>
    /// Gets the collection of steps that failed.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="IStep"/> instances representing the failed steps.
    /// </value>
    public IReadOnlyCollection<IStep> FailedSteps { get; }

    /// <inheritdoc/>
    public override string Message => Error;

    [field: AllowNull, MaybeNull]
    private string Error
    {
        get
        {
            if (field is not null)
                return field;
            var stringBuilder = new StringBuilder($"{FailedSteps.Count} Failed Step(s)");
            if (FailedSteps.Count > 0)
            {
                stringBuilder.Append(':');
                stringBuilder.Append(' ');
            }
            foreach (var step in FailedSteps)
                stringBuilder.Append($"Step '{step}' failed with error: {step.Error?.Message ?? "n/a"};");
            field = stringBuilder.ToString().TrimEnd(';');
            return field;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StepFailureException"/> class with steps that failed.
    /// </summary>
    /// <param name="failedSteps">The failed steps.</param>
    public StepFailureException(IEnumerable<IStep> failedSteps)
    {
        if (failedSteps == null) 
            throw new ArgumentNullException(nameof(failedSteps));
        FailedSteps = failedSteps.ToList();
    }
}