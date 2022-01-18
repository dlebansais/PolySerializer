namespace PolySerializer;

/// <summary>
/// Defines how objects are serialized and deserialized.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// When serializing, use a binary format.
    /// When deserializing, accept binary or human-readable formats.
    /// </summary>
    BinaryPreferred,

    /// <summary>
    /// When serializing, use a human-readable format.
    /// When deserializing, accept binary or human-readable formats.
    /// </summary>
    TextPreferred,

    /// <summary>
    /// When serializing, use a binary format.
    /// When deserializing, only accept binary format.
    /// </summary>
    BinaryOnly,

    /// <summary>
    /// When serializing, use a human-readable format.
    /// When deserializing, only accept human-readable formats.
    /// </summary>
    TextOnly,
}
