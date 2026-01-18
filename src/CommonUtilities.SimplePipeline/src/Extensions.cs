using System;
using System.Linq;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

internal static class Extensions
{
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

        internal T? FindException<T>() where T : Exception
        {
            return error switch
            {
                T t => t,
                AggregateException aggregateException => aggregateException.InnerExceptions
                    .Select(p => p.FindException<T>())
                    .FirstOrDefault(p => p is not null),
                _ => null
            };
        }
    }
}