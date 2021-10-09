using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Sklavenwalker.CommonUtilities.Xml;

/// <inheritdoc cref="IXmlObjectParser{T}"/>
public class XmlObjectParser<T> : IXmlObjectParser<T> where T: class
{
    /// <inheritdoc/>
    public T? Parse(Stream stream, bool keepOpen = false)
    {
        if (stream == null || stream.Length == 0)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new NotSupportedException();
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            var reader = XmlReader.Create(stream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Document });
            var result = new XmlSerializer(typeof(T)).Deserialize(reader);
            if (result is null)
                return null;
            return (T) result;
        }
        finally
        {
            if (!keepOpen)
                stream.Dispose();
        }
    }
}