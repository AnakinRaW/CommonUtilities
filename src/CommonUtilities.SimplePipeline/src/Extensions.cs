using System;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

internal static class Extensions
{
    extension(Exception error)
    {
        public bool IsExceptionType<T>() where T : Exception
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

    public static IEnumerable<IStep> WhereFailed(this IEnumerable<IStep> steps)
    {
        if (steps == null) 
            throw new ArgumentNullException(nameof(steps));
        return steps
            .Where(p => p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>());
    }
}