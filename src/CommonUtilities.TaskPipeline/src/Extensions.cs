using System;
using System.Linq;

namespace Sklavenwalker.CommonUtilities.TaskPipeline;

internal static class Extensions
{
    public static bool IsExceptionType<T>(this Exception error) where T : Exception
    {
        return error switch
        {
            T _ => true,
            AggregateException aggregateException => aggregateException.InnerExceptions.Any(p =>
                p.IsExceptionType<T>()),
            _ => false
        };
    }
}