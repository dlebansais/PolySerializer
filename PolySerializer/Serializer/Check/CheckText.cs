namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Contracts;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
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

            HandleCR(data, ref offset);

            CheckedObjectList.Clear();

            if (!ProcessCheckable_TEXT(RootType, ref data, ref offset))
                return false;

            if (RootType is null)
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

        private bool Check_TEXT(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject? nextChecked)
        {
            if (!CheckCollection_TEXT(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType)!;
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_TEXT(CheckedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in CheckedMembers)
            {
                if (MemberIndex++ > 0)
                    ReadSeparator_TEXT(ref data, ref offset);

                if (Member.HasCondition)
                {
                    bool ConditionValue = (bool)ReadFieldBool_TEXT(ref data, ref offset);
                    if (!ConditionValue)
                        continue;

                    ReadField(ref data, ref offset, 1);
                    offset++;
                }

                Type MemberType;

                if (Member.MemberInfo is FieldInfo AsFieldInfo)
                    MemberType = AsFieldInfo.FieldType;
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                    MemberType = AsPropertyInfo.PropertyType;
                }

                if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                    return false;
            }

            if (nextChecked is not null)
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

        private bool CheckBasicType_TEXT(Type? valueType, ref byte[] data, ref int offset)
        {
            IniReadFieldHandlerTable_TEXT();

            string? ValueName = valueType?.Name;

            if (ValueName is not null && ReadFieldHandlerTable_TEXT.ContainsKey(ValueName))
            {
                ReadFieldHandlerTable_TEXT[ValueName](ref data, ref offset);
                return true;
            }
            else if (valueType is not null && valueType.IsEnum)
            {
                CheckEnumType_TEXT(valueType, ref data, ref offset);
                return true;
            }
            else
                return false;
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
                default:
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
            }
        }

        private bool ProcessCheckable_TEXT(Type? referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_TEXT(referenceType, ref data, ref offset))
                return true;

            if (!ReadFieldType_TEXT(ref data, ref offset, out string? ReferenceTypeName))
                return false;

            if (ReferenceTypeName is null)
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

            if (ReferenceType is null)
                return false;

            Type OriginalType = ReferenceType;
            OverrideType(ref ReferenceType);
            ReferenceType = OriginalType;

            if (ReferenceType.IsValueType)
                return Check_TEXT(ReferenceType, -1, ref data, ref offset, null);
            else
                return ProcessCheckableReferenceType_TEXT(ReferenceType, ref data, ref offset);
        }

        private bool ProcessCheckableReferenceType_TEXT(Type referenceType, ref byte[] data, ref int offset)
        {
            ObjectTag ReferenceTag = ReadFieldTag_TEXT(ref data, ref offset);

            if (ReferenceTag == ObjectTag.ObjectIndex)
            {
                ReadFieldObjectIndex_TEXT(ref data, ref offset);
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

                        PropertyInfo AsPropertyInfo = (PropertyInfo)ConstructorParameters[i].MemberInfo;

                        Type MemberType = AsPropertyInfo.PropertyType;
                        if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                            return false;
                    }

                    ReadSeparator_TEXT(ref data, ref offset);

                    AddCheckedObject(referenceType, -1);
                }
            }
            else
            {
                AddCheckedObject(referenceType, -1);
            }

            return true;
        }
    }
}
