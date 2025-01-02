using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Represents the result of an XML validation.
/// </summary>
public sealed class XmlValidationResult : IEnumerable<XmlValidationError>
{
    private readonly IEnumerable<XmlValidationError> _errors;

    /// <summary>
    /// Gets a value that indicates whether the XML document is valid.
    /// </summary>
    public bool IsValid => !_errors.Any() || Exception is not null;

    /// <summary>
    /// Gets the exception which occurred during the XML validation or <see langword="null"/> if no exception occurred.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlValidationResult"/> class with the inner exception that is the cause for the failed XML validation.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    public XmlValidationResult(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _errors = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlValidationResult"/> class with a collection of <see cref="XmlValidationError"/>.
    /// </summary>
    /// <param name="errors"></param>
    public XmlValidationResult(IEnumerable<XmlValidationError> errors)
    {
        _errors = errors.ToList();
    }

    /// <inheritdoc/>
    public IEnumerator<XmlValidationError> GetEnumerator()
    {
        return _errors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}