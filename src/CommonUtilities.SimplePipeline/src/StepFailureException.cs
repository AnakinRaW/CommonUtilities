using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a set of errors during pipeline execution with one or multiple steps.
/// </summary>
public class StepFailureException : Exception
{
    private readonly string? _error;
    private readonly IEnumerable<IStep>? _failedSteps;

    /// <inheritdoc/>
    public override string Message => Error;
    
    private string Error
    {
        get
        {
            if (_error != null)
                return _error;
            var stringBuilder = new StringBuilder();
            if (_failedSteps != null)
            {
                foreach (var step in _failedSteps)
                    stringBuilder.Append("Step '" + step + $"' failed with error: {step.Error?.Message};");
            }
            return stringBuilder.ToString().TrimEnd(';');
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StepFailureException"/> with steps that failed
    /// </summary>
    /// <param name="failedSteps"></param>
    public StepFailureException(IEnumerable<IStep>? failedSteps)
    {
        _failedSteps = failedSteps;
    }

    /// <inheritdoc/>
    protected StepFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        _error = info.GetString(nameof(Error));
    }

    /// <inheritdoc/>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("Error", Error);
    }
}