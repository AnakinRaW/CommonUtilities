namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Represents an XML Validation error.
/// </summary>
public sealed class XmlValidationError
{
    /// <summary>
    /// Gets the message of the XML validation error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the line number where the XML validation occurred.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the position in a line where the XML validation occurred.
    /// </summary>
    public int LinePosition { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlValidationError"/> class.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <param name="line">The line of the error.</param>
    /// <param name="position">The line position of the error.</param>
    public XmlValidationError(string message, int line, int position)
    {
        Message = message;
        LineNumber = line;
        LinePosition = position;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"XML validation error at {LineNumber}:{LinePosition}: {Message}";
    }
}