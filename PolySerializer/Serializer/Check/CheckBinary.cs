namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Contracts;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
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

        private bool Check_BINARY(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject? nextChecked)
        {
            if (!CheckCollection_BINARY(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType)!;
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_BINARY(CheckedType, ref data, ref offset);

            foreach (DeserializedMember Member in CheckedMembers)
            {
                if (Member.HasCondition)
                {
                    bool ConditionValue = (bool)ReadFieldBool_BINARY(ref data, ref offset);
                    if (!ConditionValue)
                        continue;
                }

                Type MemberType;

                if (Member.MemberInfo is FieldInfo AsFieldInfo)
                    MemberType = AsFieldInfo.FieldType;
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
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

        private bool CheckBasicType_BINARY(Type? valueType, ref byte[] data, ref int offset)
        {
            IniReadFieldHandlerTable_BINARY();

            string? ValueName = valueType?.Name;

            if (ValueName != null && ReadFieldHandlerTable_BINARY.ContainsKey(ValueName))
            {
                ReadFieldHandlerTable_BINARY[ValueName](ref data, ref offset);
                return true;
            }
            else if (valueType != null && valueType.IsEnum)
            {
                CheckEnumType_BINARY(valueType, ref data, ref offset);
                return true;
            }
            else
                return false;
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
                default:
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
            }
        }

        private bool ProcessCheckable_BINARY(Type? referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_BINARY(referenceType, ref data, ref offset))
                return true;

            string? ReferenceTypeName = ReadFieldType_BINARY(ref data, ref offset);
            if (ReferenceTypeName == null)
                return true;

            OverrideTypeName(ref ReferenceTypeName);

            Type? ReferenceType;

            try
            {
                ReferenceType = Type.GetType(ReferenceTypeName);
            }
            catch (SystemException)
            {
                ReferenceType = null;
            }

            if (ReferenceType == null)
                return false;

            Type OriginalType = ReferenceType;
            OverrideType(ref ReferenceType);
            ReferenceType = OriginalType;

            if (ReferenceType.IsValueType)
                return Check_BINARY(ReferenceType, -1, ref data, ref offset, null);
            else
                return ProcessCheckableReferenceType_BINARY(ReferenceType, ref data, ref offset);
        }

        private bool ProcessCheckableReferenceType_BINARY(Type referenceType, ref byte[] data, ref int offset)
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
                        PropertyInfo AsPropertyInfo = (PropertyInfo)ConstructorParameters[i].MemberInfo;

                        Type MemberType = AsPropertyInfo.PropertyType;
                        if (!ProcessCheckable_BINARY(MemberType, ref data, ref offset))
                            return false;
                    }

                    AddCheckedObject(referenceType, -1);
                }
            }

            return true;
        }
    }
}
