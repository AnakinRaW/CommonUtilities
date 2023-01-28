using System;
using System.Runtime.Serialization;

namespace AnakinRaW.CommonUtilities.TaskPipeline;

/// <summary>
/// Exception, indication something regarding <see cref="IRunner"/> or <see cref="ITask"/> went wrong
/// </summary>
[Serializable]
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
    /// Initializes a new instance of the <see cref="RunnerException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public RunnerException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerException"/> class with serialized data.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected RunnerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}