namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private object INTERNAL_Deserialize_BINARY(ref byte[] data, ref int offset)
        {
            Mode = (SerializationMode)BitConverter.ToInt32(data, offset);
            offset += 4;

            DeserializedObjectList.Clear();

            object Reference;
            ProcessDeserializable_BINARY(RootType, ref data, ref offset, out Reference);

            Root = Reference;

            if (RootType == null)
                RootType = Root.GetType();

            int i = 0;
            while (i < DeserializedObjectList.Count)
            {
                Progress = i / (double)DeserializedObjectList.Count;

                IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
                Reference = NextDeserialized.Reference;
                Deserialize_BINARY(ref Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref data, ref offset, NextDeserialized);
            }

            Progress = 1.0;

            return Root;
        }

        private void Deserialize_BINARY(ref object reference, Type referenceType, long count, ref byte[] data, ref int offset, IDeserializedObject nextDeserialized)
        {
            DeserializeCollection_BINARY(ref reference, referenceType, count, ref data, ref offset);

            Type DeserializedType = SerializableAncestor(referenceType);
            List<DeserializedMember> DeserializedMembers = ListDeserializedMembers_BINARY(DeserializedType, ref data, ref offset);

            foreach (DeserializedMember Member in DeserializedMembers)
            {
                if (Member.HasCondition)
                {
                    bool ConditionValue = ReadFieldBool_BINARY(ref data, ref offset);
                    if (!ConditionValue)
                        continue;
                }

                object MemberValue;
                Type MemberType;

                FieldInfo AsFieldInfo;
                PropertyInfo AsPropertyInfo;

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    MemberType = AsFieldInfo.FieldType;
                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    MemberType = AsPropertyInfo.PropertyType;
                }

                ProcessDeserializable_BINARY(MemberType, ref data, ref offset, out MemberValue);

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    AsFieldInfo.SetValue(reference, MemberValue);
                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    if (Member.PropertySetter == null)
                        AsPropertyInfo.SetValue(reference, MemberValue);
                    else
                        Member.PropertySetter.Invoke(reference, new object[] { MemberValue });
                }
            }

            if (nextDeserialized != null)
                nextDeserialized.SetDeserialized();
        }

        private void DeserializeCollection_BINARY(ref object reference, Type referenceType, long count, ref byte[] data, ref int offset)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(reference, referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        object Item;
                        ProcessDeserializable_BINARY(ItemType, ref data, ref offset, out Item);

                        Inserter.AddItem(Item);
                    }
                }
            }
        }

        private bool DeserializeBasicType_BINARY(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            switch (valueType?.Name)
            {
                case nameof(SByte):
                    value = ReadFieldSByte_BINARY(ref data, ref offset);
                    break;
                case nameof(Byte):
                    value = ReadFieldByte_BINARY(ref data, ref offset);
                    break;
                case nameof(Boolean):
                    value = ReadFieldBool_BINARY(ref data, ref offset);
                    break;
                case nameof(Char):
                    value = ReadFieldChar_BINARY(ref data, ref offset);
                    break;
                case nameof(Decimal):
                    value = ReadFieldDecimal_BINARY(ref data, ref offset);
                    break;
                case nameof(Double):
                    value = ReadFieldDouble_BINARY(ref data, ref offset);
                    break;
                case nameof(Single):
                    value = ReadFieldFloat_BINARY(ref data, ref offset);
                    break;
                case nameof(Int32):
                    value = ReadFieldInt_BINARY(ref data, ref offset);
                    break;
                case nameof(Int64):
                    value = ReadFieldLong_BINARY(ref data, ref offset);
                    break;
                case nameof(Int16):
                    value = ReadFieldShort_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt32):
                    value = ReadFieldUInt_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt64):
                    value = ReadFieldULong_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt16):
                    value = ReadFieldUShort_BINARY(ref data, ref offset);
                    break;
                case nameof(String):
                    value = ReadFieldString_BINARY(ref data, ref offset);
                    break;
                case nameof(Guid):
                    value = ReadFieldGuid_BINARY(ref data, ref offset);
                    break;
                default:
                    if (valueType != null && valueType.IsEnum)
                        DeserializeEnumType_BINARY(valueType, ref data, ref offset, out value);
                    else
                    {
                        value = null;
                        return false;
                    }
                    break;
            }

            return true;
        }

        private void DeserializeEnumType_BINARY(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    value = Enum.ToObject(valueType, ReadFieldSByte_BINARY(ref data, ref offset));
                    break;
                case nameof(Byte):
                    value = Enum.ToObject(valueType, ReadFieldByte_BINARY(ref data, ref offset));
                    break;
                case nameof(Int16):
                    value = Enum.ToObject(valueType, ReadFieldShort_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt16):
                    value = Enum.ToObject(valueType, ReadFieldUShort_BINARY(ref data, ref offset));
                    break;
                case nameof(Int32):
                    value = Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt32):
                    value = Enum.ToObject(valueType, ReadFieldUInt_BINARY(ref data, ref offset));
                    break;
                case nameof(Int64):
                    value = Enum.ToObject(valueType, ReadFieldLong_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt64):
                    value = Enum.ToObject(valueType, ReadFieldULong_BINARY(ref data, ref offset));
                    break;
                default:
                    value = Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
                    break;
            }
        }

        private void ProcessDeserializable_BINARY(Type referenceType, ref byte[] data, ref int offset, out object reference)
        {
            if (DeserializeBasicType_BINARY(referenceType, ref data, ref offset, out reference))
                return;

            string ReferenceTypeName = ReadFieldType_BINARY(ref data, ref offset);
            if (ReferenceTypeName == null)
            {
                reference = null;
                return;
            }

            OverrideTypeName(ref ReferenceTypeName);
            referenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = referenceType;
            OverrideType(ref referenceType);
            Type NewType = referenceType;
            referenceType = OriginalType;

            if (referenceType.IsValueType)
            {
                CreateObject(NewType, ref reference);
                Deserialize_BINARY(ref reference, referenceType, -1, ref data, ref offset, null);
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_BINARY(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    int ReferenceIndex = ReadFieldObjectIndex_BINARY(ref data, ref offset);
                    reference = DeserializedObjectList[ReferenceIndex].Reference;
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    CreateObject(NewType, ref reference);
                    AddDeserializedObject(reference, referenceType, -1);
                }
                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount_BINARY(ref data, ref offset);

                    CreateObject(NewType, Count, ref reference);
                    AddDeserializedObject(reference, referenceType, Count);
                }
                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(referenceType, out ConstructorParameters))
                    {
                        object[] Parameters = new object[ConstructorParameters.Count];

                        for (int i = 0; i < ConstructorParameters.Count; i++)
                        {
                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            object MemberValue;
                            Type MemberType = AsPropertyInfo.PropertyType;
                            ProcessDeserializable_BINARY(MemberType, ref data, ref offset, out MemberValue);

                            Parameters[i] = MemberValue;
                        }

                        CreateObject(NewType, Parameters, ref reference);
                        AddDeserializedObject(reference, referenceType, -1);
                    }
                }
            }
        }

        private List<DeserializedMember> ListDeserializedMembers_BINARY(Type deserializedType, ref byte[] data, ref int offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                List<string> MemberNames = ReadFieldMembers_BINARY(ref data, ref offset);
                foreach (string MemberName in MemberNames)
                {
                    MemberInfo[] MatchingMembers = deserializedType.GetMember(MemberName);
                    DeserializedMember NewMember = new DeserializedMember(MatchingMembers[0]);

                    CheckForSerializedCondition(NewMember);
                    DeserializedMembers.Add(NewMember);
                }
            }
            else
            {
                List<MemberInfo> Members = new List<MemberInfo>(deserializedType.GetMembers());

                foreach (MemberInfo MemberInfo in Members)
                {
                    DeserializedMember NewMember = new DeserializedMember(MemberInfo);

                    if (!IsDeserializableMember(deserializedType, NewMember))
                        continue;

                    DeserializedMembers.Add(NewMember);
                }

                if (Mode == SerializationMode.Default)
                    DeserializedMembers.Sort(SortByName);
            }

            return DeserializedMembers;
        }
    }
}
