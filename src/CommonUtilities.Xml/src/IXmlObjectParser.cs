using System;
using System.IO;

namespace AnakinRaW.CommonUtilities.Xml;

/// <summary>
/// Generic XML Deserializer service.
/// </summary>
/// <typeparam name="T">The target result type.</typeparam>
public interface IXmlObjectParser<out T> where T: class
{
    /// <summary>
    /// Deserializes the XML document contained by the specified Stream.
    /// </summary>
    /// <param name="stream">The Stream that contains the XML document to deserialize.</param>
    /// <returns>The Object being deserialized casted to <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Cannot read from <paramref name="stream"/>.</exception>
    public T? Parse(Stream stream);
}