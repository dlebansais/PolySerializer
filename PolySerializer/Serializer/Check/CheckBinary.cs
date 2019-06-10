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
        private bool INTERNAL_Check_BINARY(ref byte[] data, ref int offset)
        {
            Mode = (SerializationMode)BitConverter.ToInt32(data, offset);
            offset += 4;

            CheckedObjectList.Clear();

            if (!ProcessCheckable_BINARY(RootType, ref data, ref offset))
                return false;

            if (RootType == null)
                return false;

            bool Success = true;
            int i = 0;
            while (i < CheckedObjectList.Count)
            {
                Progress = i / (double)CheckedObjectList.Count;

                ICheckedObject NextChecked = CheckedObjectList[i++];
                if (!Check_BINARY(NextChecked.CheckedType, NextChecked.Count, ref data, ref offset, NextChecked))
                {
                    Success = false;
                    break;
                }
            }

            Progress = 1.0;

            return Success;
        }

        private bool Check_BINARY(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject nextChecked)
        {
            if (!CheckCollection_BINARY(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType);
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_BINARY(CheckedType, ref data, ref offset);

            foreach (DeserializedMember Member in CheckedMembers)
            {
                if (Member.HasCondition)
                {
                    bool ConditionValue = ReadFieldBool_BINARY(ref data, ref offset);
                    if (!ConditionValue)
                        continue;
                }

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

                if (!ProcessCheckable_BINARY(MemberType, ref data, ref offset))
                    return false;
            }

            if (nextChecked != null)
                nextChecked.SetChecked();

            return true;
        }

        private bool CheckCollection_BINARY(Type referenceType, long count, ref byte[] data, ref int offset)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (!ProcessCheckable_BINARY(ItemType, ref data, ref offset))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool CheckBasicType_BINARY(Type valueType, ref byte[] data, ref int offset)
        {
            switch (valueType?.Name)
            {
                case nameof(SByte):
                    ReadFieldSByte_BINARY(ref data, ref offset);
                    break;
                case nameof(Byte):
                    ReadFieldByte_BINARY(ref data, ref offset);
                    break;
                case nameof(Boolean):
                    ReadFieldBool_BINARY(ref data, ref offset);
                    break;
                case nameof(Char):
                    ReadFieldChar_BINARY(ref data, ref offset);
                    break;
                case nameof(Decimal):
                    ReadFieldDecimal_BINARY(ref data, ref offset);
                    break;
                case nameof(Double):
                    ReadFieldDouble_BINARY(ref data, ref offset);
                    break;
                case nameof(Single):
                    ReadFieldFloat_BINARY(ref data, ref offset);
                    break;
                case nameof(Int32):
                    ReadFieldInt_BINARY(ref data, ref offset);
                    break;
                case nameof(Int64):
                    ReadFieldLong_BINARY(ref data, ref offset);
                    break;
                case nameof(Int16):
                    ReadFieldShort_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt32):
                    ReadFieldUInt_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt64):
                    ReadFieldULong_BINARY(ref data, ref offset);
                    break;
                case nameof(UInt16):
                    ReadFieldUShort_BINARY(ref data, ref offset);
                    break;
                case nameof(String):
                    ReadFieldString_BINARY(ref data, ref offset);
                    break;
                case nameof(Guid):
                    ReadFieldGuid_BINARY(ref data, ref offset);
                    break;
                default:
                    if (valueType != null && valueType.IsEnum)
                        CheckEnumType_BINARY(valueType, ref data, ref offset);
                    else
                        return false;
                    break;
            }

            return true;
        }

        private void CheckEnumType_BINARY(Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    Enum.ToObject(valueType, ReadFieldSByte_BINARY(ref data, ref offset));
                    break;
                case nameof(Byte):
                    Enum.ToObject(valueType, ReadFieldByte_BINARY(ref data, ref offset));
                    break;
                case nameof(Int16):
                    Enum.ToObject(valueType, ReadFieldShort_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt16):
                    Enum.ToObject(valueType, ReadFieldUShort_BINARY(ref data, ref offset));
                    break;
                case nameof(Int32):
                    Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt32):
                    Enum.ToObject(valueType, ReadFieldUInt_BINARY(ref data, ref offset));
                    break;
                case nameof(Int64):
                    Enum.ToObject(valueType, ReadFieldLong_BINARY(ref data, ref offset));
                    break;
                case nameof(UInt64):
                    Enum.ToObject(valueType, ReadFieldULong_BINARY(ref data, ref offset));
                    break;
                default:
                    Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
                    break;
            }
        }

        private bool ProcessCheckable_BINARY(Type referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_BINARY(referenceType, ref data, ref offset))
                return true;

            string ReferenceTypeName = ReadFieldType_BINARY(ref data, ref offset);
            if (ReferenceTypeName == null)
                return true;

            OverrideTypeName(ref ReferenceTypeName);
            referenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = referenceType;
            OverrideType(ref referenceType);
            Type NewType = referenceType;
            referenceType = OriginalType;

            if (referenceType.IsValueType)
            {
                if (!Check_BINARY(referenceType, -1, ref data, ref offset, null))
                    return false;
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_BINARY(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    ReadFieldObjectIndex_BINARY(ref data, ref offset);
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    AddCheckedObject(referenceType, -1);
                }
                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount_BINARY(ref data, ref offset);
                    AddCheckedObject(referenceType, Count);
                }
                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(referenceType, out ConstructorParameters))
                    {
                        for (int i = 0; i < ConstructorParameters.Count; i++)
                        {
                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            Type MemberType = AsPropertyInfo.PropertyType;
                            if (!ProcessCheckable_BINARY(MemberType, ref data, ref offset))
                                return false;
                        }

                        AddCheckedObject(referenceType, -1);
                    }
                }
            }

            return true;
        }
    }
}
