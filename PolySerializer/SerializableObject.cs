namespace PolySerializer;

using System;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name
internal interface ISerializableObject
#pragma warning restore SA1649 // File name should match first type name
{
    object Reference { get; }
    Type ReferenceType { get; }
    long Count { get; }
    void SetSerialized();
}

internal class SerializableObject : ISerializableObject
{
    #region Init
    public SerializableObject(object reference, Type referenceType, long count)
    {
        Reference = reference;
        ReferenceType = referenceType;
        Count = count;
        IsSerialized = false;
    }
    #endregion

    #region Properties
    public object Reference { get; private set; }
    public Type ReferenceType { get; private set; }
    public long Count { get; private set; }
    public bool IsSerialized { get; private set; }
    #endregion

    #region Client Interface
    public void SetSerialized()
    {
        IsSerialized = true;
    }
    #endregion

    #region Debugging
    public override string ToString()
    {
        string Result = $"{Reference} as {ReferenceType.FullName}";

        if (Count >= 0)
            Result += $" (*{Count})";

        if (IsSerialized)
            Result += " (Serialized)";

        return Result;
    }
    #endregion
}
#pragma warning restore SA1600 // Elements should be documented
