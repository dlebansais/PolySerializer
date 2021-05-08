namespace PolySerializer
{
    using System;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name
    internal interface IDeserializedObject
#pragma warning restore SA1649 // File name should match first type name
    {
        object Reference { get; }
        Type DeserializedType { get; }
        long Count { get; }
        void SetDeserialized();
    }

    internal class DeserializedObject : IDeserializedObject
    {
        #region Init
        public DeserializedObject(object reference, Type deserializedType, long count)
        {
            Reference = reference;
            DeserializedType = deserializedType;
            Count = count;
            IsDeserialized = false;
        }
        #endregion

        #region Properties
        public object Reference { get; private set; }
        public Type DeserializedType { get; private set; }
        public long Count { get; private set; }
        public bool IsDeserialized { get; private set; }
        #endregion

        #region Client Interface
        public void SetDeserialized()
        {
            IsDeserialized = true;
        }
        #endregion

        #region Debugging
        public override string ToString()
        {
            string Result = $"{Reference} as {DeserializedType.FullName}";

            if (IsDeserialized)
                Result += " (Deserialized)";

            return Result;
        }
        #endregion
    }
#pragma warning restore SA1600 // Elements should be documented
}
