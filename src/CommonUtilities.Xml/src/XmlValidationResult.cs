using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Data which represents the result of an XML validation.
/// </summary>
public sealed class XmlValidationResult : IEnumerable<XmlValidationError>
{
    private readonly IEnumerable<XmlValidationError> _errors;

    /// <summary>
    /// Indicates whether the XML document is valid.
    /// </summary>
    public bool IsValid => !_errors.Any() || Exception is not null;

    /// <summary>
    /// An Error which occurred during the XML validation.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Creates a new <see cref="XmlValidationResult"/> caused by an XML validation exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    public XmlValidationResult(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _errors = Enumerable.Empty<XmlValidationError>();
    }

    /// <summary>
    /// Creates a new <see cref="XmlValidationResult"/> with an collection of <see cref="XmlValidationError"/>.
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