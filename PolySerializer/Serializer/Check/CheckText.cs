namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private bool INTERNAL_Check_TEXT(ref byte[] data, ref int offset)
        {
            offset += 4;

            ReadField(ref data, ref offset, 8);
            string s = Encoding.UTF8.GetString(data, offset, 8).Substring(1, 7);

            if (s == SerializationMode.Default.ToString())
            {
                Mode = SerializationMode.Default;
                offset += 9;
            }
            else if (s == SerializationMode.MemberName.ToString().Substring(0, 7))
            {
                Mode = SerializationMode.MemberName;
                offset += 12;
            }
            else if (s == SerializationMode.MemberOrder.ToString().Substring(0, 7))
            {
                Mode = SerializationMode.MemberOrder;
                offset += 13;
            }
            else
                throw new InvalidDataException("Mode");

            CheckedObjectList.Clear();

            if (!ProcessCheckable_TEXT(RootType, ref data, ref offset))
                return false;

            if (RootType == null)
                return false;

            bool Success = true;
            int i = 0;
            while (i < CheckedObjectList.Count)
            {
                Progress = i / (double)CheckedObjectList.Count;

                ICheckedObject NextChecked = CheckedObjectList[i++];
                if (!Check_TEXT(NextChecked.CheckedType, NextChecked.Count, ref data, ref offset, NextChecked))
                {
                    Success = false;
                    break;
                }
            }

            Progress = 1.0;

            return Success;
        }

        private bool Check_TEXT(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject nextChecked)
        {
            if (!CheckCollection_TEXT(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType);
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_TEXT(CheckedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in CheckedMembers)
            {
                if (MemberIndex++ > 0)
                    ReadSeparator_TEXT(ref data, ref offset);

                if (Member.HasCondition)
                {
                    bool ConditionValue = ReadFieldBool_TEXT(ref data, ref offset);
                    if (!ConditionValue)
                        continue;

                    ReadField(ref data, ref offset, 1);
                    offset++;
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

                if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                    return false;
            }

            if (nextChecked != null)
                nextChecked.SetChecked();

            return true;
        }

        private bool CheckCollection_TEXT(Type referenceType, long count, ref byte[] data, ref int offset)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (i > 0)
                            ReadSeparator_TEXT(ref data, ref offset);

                        if (!ProcessCheckable_TEXT(ItemType, ref data, ref offset))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool CheckBasicType_TEXT(Type valueType, ref byte[] data, ref int offset)
        {
            switch (valueType?.Name)
            {
                case nameof(SByte):
                    ReadFieldSByte_TEXT(ref data, ref offset);
                    break;
                case nameof(Byte):
                    ReadFieldByte_TEXT(ref data, ref offset);
                    break;
                case nameof(Boolean):
                    ReadFieldBool_TEXT(ref data, ref offset);
                    break;
                case nameof(Char):
                    ReadFieldChar_TEXT(ref data, ref offset);
                    break;
                case nameof(Decimal):
                    ReadFieldDecimal_TEXT(ref data, ref offset);
                    break;
                case nameof(Double):
                    ReadFieldDouble_TEXT(ref data, ref offset);
                    break;
                case nameof(Single):
                    ReadFieldFloat_TEXT(ref data, ref offset);
                    break;
                case nameof(Int32):
                    ReadFieldInt_TEXT(ref data, ref offset);
                    break;
                case nameof(Int64):
                    ReadFieldLong_TEXT(ref data, ref offset);
                    break;
                case nameof(Int16):
                    ReadFieldShort_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt32):
                    ReadFieldUInt_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt64):
                    ReadFieldULong_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt16):
                    ReadFieldUShort_TEXT(ref data, ref offset);
                    break;
                case nameof(String):
                    ReadFieldString_TEXT(ref data, ref offset);
                    break;
                case nameof(Guid):
                    ReadFieldGuid_TEXT(ref data, ref offset);
                    break;
                default:
                    if (valueType != null && valueType.IsEnum)
                        CheckEnumType_TEXT(valueType, ref data, ref offset);
                    else
                        return false;
                    break;
            }

            return true;
        }

        private void CheckEnumType_TEXT(Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    Enum.ToObject(valueType, ReadFieldSByte_TEXT(ref data, ref offset));
                    break;
                case nameof(Byte):
                    Enum.ToObject(valueType, ReadFieldByte_TEXT(ref data, ref offset));
                    break;
                case nameof(Int16):
                    Enum.ToObject(valueType, ReadFieldShort_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt16):
                    Enum.ToObject(valueType, ReadFieldUShort_TEXT(ref data, ref offset));
                    break;
                case nameof(Int32):
                    Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt32):
                    Enum.ToObject(valueType, ReadFieldUInt_TEXT(ref data, ref offset));
                    break;
                case nameof(Int64):
                    Enum.ToObject(valueType, ReadFieldLong_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt64):
                    Enum.ToObject(valueType, ReadFieldULong_TEXT(ref data, ref offset));
                    break;
                default:
                    Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
                    break;
            }
        }

        private bool ProcessCheckable_TEXT(Type referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_TEXT(referenceType, ref data, ref offset))
                return true;

            string ReferenceTypeName = ReadFieldType_TEXT(ref data, ref offset);
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
                if (!Check_TEXT(referenceType, -1, ref data, ref offset, null))
                    return false;
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_TEXT(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    ReadFieldObjectIndex_TEXT(ref data, ref offset);
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    AddCheckedObject(referenceType, -1);
                }
                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount_TEXT(ref data, ref offset);
                    AddCheckedObject(referenceType, Count);
                }
                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(referenceType, out ConstructorParameters))
                    {
                        for (int i = 0; i < ConstructorParameters.Count; i++)
                        {
                            if (i > 0)
                                ReadSeparator_TEXT(ref data, ref offset);

                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            Type MemberType = AsPropertyInfo.PropertyType;
                            if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                                return false;
                        }

                        ReadSeparator_TEXT(ref data, ref offset);

                        AddCheckedObject(referenceType, -1);
                    }
                }
            }

            return true;
        }
    }
}
