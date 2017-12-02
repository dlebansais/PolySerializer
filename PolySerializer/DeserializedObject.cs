﻿using System;

namespace PolySerializer
{
    #region Interface
    internal interface IDeserializedObject
    {
        object Reference { get; }
        Type DeserializedType { get; }
        long Count { get; }
        void SetDeserialized();
    }
    #endregion

    internal class DeserializedObject : IDeserializedObject
    {
        #region Init
        public DeserializedObject(object Reference, Type DeserializedType, long Count)
        {
            this.Reference = Reference;
            this.DeserializedType = DeserializedType;
            this.Count = Count;
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
            string Result;
            if (Reference == null)
                Result = "null for " + DeserializedType.FullName;
            else
                Result = Reference.ToString() + " as " + DeserializedType.FullName;

            if (IsDeserialized)
                Result += " (Deserialized)";

            return Result;
        }
        #endregion
    }
}
