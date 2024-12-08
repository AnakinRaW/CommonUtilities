using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AnakinRaW.CommonUtilities.Xml;

/// <inheritdoc cref="IXmlObjectParser{T}"/>
public sealed class XmlObjectParser<T> : IXmlObjectParser<T> where T: class
{
    /// <inheritdoc/>
    public T? Parse(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new NotSupportedException();

        var reader = XmlReader.Create(stream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Document });
        var result = new XmlSerializer(typeof(T)).Deserialize(reader);
        return (T)result;
    }
}