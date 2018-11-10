namespace PolySerializer
{
    /// <summary>
    ///     Defines how objects are serialized and deserialized.
    /// </summary>
    public enum SerializationMode
    {
        /// <summary>
        ///     When serializing, members are sorted by name and serialized in that order.
        ///     When deserializing, the same sorted order is used. This mode supports recompiling the code, but not adding or removing members.
        /// </summary>
        Default,

        /// <summary>
        ///     When serializing, the name of each member is saved along with its value. This outputs larger serialized data.
        ///     When deserializing, The member name is searched in the deserialized type. If not found, deserialization fails.
        ///     This method supports deserializing to a type with more members than the original type did use to serialize.
        /// </summary>
        MemberName,

        /// <summary>
        ///     Members are matched in the order they are found in serialized data and in the type.
        ///     This is faster than <see cref="MemberName"/>, and supports renaming members, but the source and destination types must match exactly.
        ///     This also supports a destination type with more members then the original, if they appear at the end.
        /// </summary>
        MemberOrder,
    }
}
