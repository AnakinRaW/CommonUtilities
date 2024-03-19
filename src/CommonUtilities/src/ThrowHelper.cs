﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AnakinRaW.CommonUtilities;

/// <summary>
/// Contains helpers for throwing common exceptions.
/// </summary>
public static class ThrowHelper
{
    /// <summary>Throws an exception if <paramref name="argument"/> is null or empty.</summary>
    /// <param name="argument">The string argument to validate as non-null and non-empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="argument"/> is empty.</exception>
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
#else
        if (string.IsNullOrEmpty(argument))
            ThrowNullOrEmptyException(argument, paramName);
#endif

    }

    /// <summary>Throws an exception if <paramref name="argument"/> is null or empty.</summary>
    /// <param name="argument">The collection argument to validate as non-null and non-empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="argument"/> is empty.</exception>
    public static void ThrowIfCollectionNullOrEmpty([NotNull] ICollection? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Count == 0)
            throw new ArgumentException("The value cannot be an empty collection.", paramName);
    }

    /// <summary>Throws an exception if <paramref name="argument"/> is null or empty.</summary>
    /// <param name="argument">The collection argument to validate as non-null and non-empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="argument"/> is empty.</exception>
    public static void ThrowIfCollectionNullOrEmpty<T>([NotNull] ICollection<T>? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Count == 0)
            throw new ArgumentException("The value cannot be an empty collection.", paramName);
    }

#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.


    /// <summary>
    /// Throws an exception if <paramref name="argument"/> is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="argument">The string argument to validate.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="argument"/> is empty or consists only of white-space characters.</exception>
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
        if (string.IsNullOrWhiteSpace(argument))
            ThrowNullOrWhiteSpaceException(argument, paramName);
#endif
    }
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.

    [DoesNotReturn]
    private static void ThrowNullOrWhiteSpaceException(string? argument, string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
    }

    [DoesNotReturn]
    private static void ThrowNullOrEmptyException(string? argument, string? paramName)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        throw new ArgumentException("The value cannot be an empty string.", paramName);
    }
}