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
    /// <param name="keepOpen">When set to <see langword="true"/> the <paramref name="stream"/> will not get disposed after the operation.
    /// By default, the <paramref name="stream"/> will get disposed.</param>
    /// <returns>The Object being deserialized casted to <typeparamref name="T"/>.</returns>
    public T? Parse(Stream stream, bool keepOpen = false);
}