using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace AnakinRaW.CommonUtilities.Testing.Extensions;

public static class AssertExtensions
{
    private static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

    extension(Assert)
    {
        /// <summary>
        /// Verifies that the specified action does not throw any exception.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the action.</typeparam>
        /// <param name="action">A delegate to the code to be tested.</param>
        /// <returns>The result of the executed test code.</returns>
        public static T DoesNotThrowException<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected no exception to be thrown but got '{e.GetType().Name}' instead");
                return default;
            }
        }

        /// <summary>
        /// Verifies that the specified action does not throw any exception.
        /// </summary>
        /// <param name="action">A delegate to the code to be tested.</param>
        public static void DoesNotThrowException(Action action)
        {
            Assert.DoesNotThrowException(() => action);
        }

        public static T Throws<T>(string? expectedParamName, Action action) where T : ArgumentException
        {
            var exception = Assert.Throws<T>(action);
            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        public static T Throws<T>(string netCoreParamName, string? netFxParamName, Action action)
            where T : ArgumentException
        {
            var exception = Assert.Throws<T>(action);

            if (netFxParamName == null && IsNetFramework)
            {
                // Param name varies between .NET Framework versions -- skip checking it
                return exception;
            }

            var expectedParamName = IsNetFramework ? netFxParamName : netCoreParamName;

            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        // From https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Collections/CollectionAsserts.cs

        public static void EqualUnordered(ICollection expected, ICollection actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
                return;

            // Lookups are an aggregated collections (enumerable contents), but ordered.
            var e = expected.Cast<object>().ToLookup(key => key);
            var a = actual.Cast<object>().ToLookup(key => key);

            // Dictionaries can't handle null keys, which is a possibility
            Assert.Equal(
                e.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()),
                a.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()));

            // Get count of null keys.  Returns an empty sequence (and thus a 0 count) if no null key
            Assert.Equal(e[null!].Count(), a[null!].Count());
        }

        public static void EqualUnordered<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
                return;

            // Lookups are an aggregated collections (enumerable contents), but ordered.
            var e = expected.Cast<object>().ToLookup(key => key);
            var a = actual!.Cast<object>().ToLookup(key => key);

            // Dictionaries can't handle null keys, which is a possibility
            Assert.Equal(
                e.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()),
                a.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()));

            // Get count of null keys.  Returns an empty sequence (and thus a 0 count) if no null key
            Assert.Equal(e[null!].Count(), a[null!].Count());
        }

        // Based on https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Exception.Helpers.cs

        /// <summary>
        /// Validates the properties of the specified <see cref="Exception"/> instance against the provided values.
        /// </summary>
        /// <param name="e">The exception instance to validate.</param>
        /// <param name="innerException">The expected inner exception of <paramref name="e"/>. Defaults to <c>null</c>.</param>
        /// <param name="message">The expected message of <paramref name="e"/>. Defaults to <c>null</c>.</param>
        /// <param name="source">The expected source of <paramref name="e"/>. Defaults to <c>null</c>.</param>
        /// <param name="stackTrace">The expected stack trace of <paramref name="e"/>. Defaults to <c>null</c>.</param>
        /// <param name="validateMessage">A value indicating whether to validate the <paramref name="message"/> property.</param>
        public static void Exception(Exception e,
            Exception? innerException = null,
            string? message = null,
            string? source = null,
            string? stackTrace = null,
            bool validateMessage = true)
        {
            Assert.Equal(innerException, e.InnerException);
            if (validateMessage)
                Assert.Equal(message, e.Message);
            else
                Assert.NotNull(e.Message);
            Assert.Equal(source, e.Source);
            Assert.Equal(stackTrace, e.StackTrace);
        }
    }
}