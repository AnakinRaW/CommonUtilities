using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a set of errors during pipeline execution with one or multiple steps.
/// </summary>
public sealed class StepFailureException : Exception
{
    private readonly IEnumerable<IStep> _failedSteps;

    /// <inheritdoc/>
    public override string Message => Error;

    [field: AllowNull, MaybeNull]
    private string Error
    {
        get
        {
            if (field is not null)
                return field;

            var stringBuilder = new StringBuilder();
            
            foreach (var step in _failedSteps)
                stringBuilder.Append($"Step '{step}' failed with error: {step.Error?.Message};");
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
        _failedSteps = failedSteps ?? throw new ArgumentNullException(nameof(failedSteps));
    }
}