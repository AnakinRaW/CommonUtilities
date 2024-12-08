using System;
using System.IO;

namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Service to Validate and XML against a XSD schema.
/// </summary>
public interface IXmlValidator
{
    /// <summary>
    /// Validates an XML file against the XSD schema of this instance.
    /// </summary>
    /// <param name="filePath">The XML file.</param>
    /// <returns>The result of the XML validation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <see langword="null"/>.</exception>
    XmlValidationResult Validate(string filePath);

    /// <summary>
    /// Validates an XML file against the XSD schema of this instance.
    /// </summary>
    /// <param name="stream">The XML document stream.</param>
    /// <returns>The result of the XML validation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    XmlValidationResult Validate(Stream stream);
}