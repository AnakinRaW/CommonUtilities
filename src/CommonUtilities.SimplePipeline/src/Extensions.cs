using System;
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
}