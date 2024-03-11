namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Represents an XML Validation error.
/// </summary>
public sealed class XmlValidationError
{
    /// <summary>
    /// The message of the XML validation error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The line number where the XML validation occurred.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// The position in a line where the XML validation occurred.
    /// </summary>
    public int LinePosition { get; }

    /// <summary>
    /// Creates a new <see cref="XmlValidationError"/>
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