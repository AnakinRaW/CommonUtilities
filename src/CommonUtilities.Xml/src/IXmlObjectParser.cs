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
    public T? Parse(Stream stream);
}