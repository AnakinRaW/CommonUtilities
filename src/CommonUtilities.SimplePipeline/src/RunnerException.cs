using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// The exception that is thrown when something regarding <see cref="IRunner"/> or <see cref="IStep"/> fails.
/// </summary>
public class RunnerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerException"/> class.
    /// </summary>
    public RunnerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerException"/> class with a specified error message.
    /// </summary>
    /// <param name="message"></param>
    public RunnerException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerException"/> class with a specified error message and
    /// a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public RunnerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}