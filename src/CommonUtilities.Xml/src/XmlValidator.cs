using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Validation;

namespace AnakinRaW.CommonUtilities.Xml;

/// <inheritdoc cref="IXmlValidator"/>
public class XmlValidator : IXmlValidator
{
    private XmlReaderSettings ReaderSettings { get; }

    /// <summary>
    /// Creates a new XML validator from a given XSD schema stream.
    /// </summary>
    /// <param name="schemeStream">The XSD schema stream of this instance.</param>
    /// <param name="conformanceLevel">The <see cref="ConformanceLevel"/> of the XSD document.</param>
    public XmlValidator(Stream schemeStream, ConformanceLevel conformanceLevel = ConformanceLevel.Auto)
    {
        Requires.NotNull(schemeStream, nameof(schemeStream));
        ReaderSettings = CreateSettings(schemeStream, conformanceLevel);
    }

    private static XmlReaderSettings CreateSettings(Stream schemeStream, ConformanceLevel conformanceLevel)
    {
        schemeStream.Seek(0, SeekOrigin.Begin);
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation |
                                    XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ConformanceLevel = conformanceLevel;
        var schemaReader = XmlReader.Create(schemeStream);
        settings.Schemas.Add(null, schemaReader);
        return settings;
    }

    /// <inheritdoc/>
    public XmlValidationResult Validate(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(nameof(filePath));
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return InternalValidate(stream);
    }

    /// <inheritdoc/>
    public XmlValidationResult Validate(Stream stream, bool keepOpen)
    {
        try
        {
            return InternalValidate(stream);
        }
        finally
        {
            if (!keepOpen)
                stream.Dispose();
        }
    }

    private XmlValidationResult InternalValidate(Stream stream)
    {
        if (!stream.CanRead)
            throw new InvalidOperationException("Cannot read from stream");
        
        stream.Seek(0, SeekOrigin.Begin);
        var errors = new List<XmlValidationError>();
        try
        { 
            ReaderSettings.ValidationEventHandler += OnValidationError;
            var reader = XmlReader.Create(stream, ReaderSettings);
            while (reader.Read())
            {
            }
            reader.Close();
        }
        catch (Exception e)
        {
            return new XmlValidationResult(e);
        }
        finally
        {
            ReaderSettings.ValidationEventHandler -= OnValidationError;
        }
        return new XmlValidationResult(errors);

        void OnValidationError(object? sender, ValidationEventArgs e)
        {
            var error = new XmlValidationError(e.Message, e.Exception.LineNumber, e.Exception.LinePosition);
            errors.Add(error);
        }
    }
}