using System;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// 
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Throws a <see cref="StepFailureException"/> if any of the provided steps have failed.
    /// </summary>
    /// <param name="executedSteps">The collection of executed steps to evaluate for failures.</param>
    /// <exception cref="StepFailureException">
    /// Thrown when one or more steps in <paramref name="executedSteps"/> have failed, 
    /// excluding those which represent a cancelled Step.
    /// </exception>
    public static void ThrowStepFailureExceptionForFailedSteps(this IEnumerable<IStep> executedSteps)
    {
        var failedBuildSteps = executedSteps
            .Where(p => p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>())
            .ToList();
        if (failedBuildSteps.Count > 0)
            throw new StepFailureException(failedBuildSteps);
    }

    extension(Exception error)
    {
        internal bool IsExceptionType<T>() where T : Exception
        {
            return error switch
            {
                T => true,
                AggregateException aggregateException => aggregateException.InnerExceptions.Any(p =>
                    p.IsExceptionType<T>()),
                _ => false
            };
        }
    }
}