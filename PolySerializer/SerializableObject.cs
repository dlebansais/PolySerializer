using System;

namespace PolySerializer
{
    #region Interface
    internal interface ISerializableObject
    {
        object Reference { get; }
        Type ReferenceType { get; }
        long Count{ get; }
        void SetSerialized();
    }
    #endregion

    internal class SerializableObject : ISerializableObject
    {
        #region Init
        public SerializableObject(object Reference, Type ReferenceType, long Count)
        {
            this.Reference = Reference;
            this.ReferenceType = ReferenceType;
            this.Count = Count;
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
            string Result;
            if (Reference == null)
                Result = "null for " + ReferenceType.Name;
            else
                Result = Reference.ToString() + " as " + ReferenceType.FullName;

            if (Count >= 0)
                Result += " (*" + Count + ")";

            if (IsSerialized)
                Result += " (Serialized)";

            return Result;
        }
        #endregion
    }
}
