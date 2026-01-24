using System;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// The exception that is thrown to signal an <see cref="IStepRunner"/> the termination of step execution.
/// </summary>
/// <remarks>
/// This exception is typically thrown by an <see cref="IStep"/> to indicate that its associated <see cref="IStepRunner"/>
/// should stop executing any further steps. 
/// </remarks>
public sealed class StopRunnerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopRunnerException"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor creates a default instance of the exception without any additional context or message.
    /// It is typically used to signal the termination of step execution in a pipeline.
    /// </remarks>
    public StopRunnerException() : base("Stopping step runner.")
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StopRunnerException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StopRunnerException(string? message) : base(message)
    {
    }
}