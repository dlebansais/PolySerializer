namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Contracts;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private object INTERNAL_Deserialize_TEXT(ref byte[] data, ref int offset)
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

            DeserializedObjectList.Clear();

            ProcessDeserializable_TEXT(RootType, ref data, ref offset, out object? Reference);

            Root = Reference!;

            if (RootType is null)
                RootType = Root.GetType();

            int i = 0;
            while (i < DeserializedObjectList.Count)
            {
                Progress = i / (double)DeserializedObjectList.Count;

                IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
                Reference = NextDeserialized.Reference;
                Deserialize_TEXT(Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref data, ref offset, NextDeserialized);
            }

            Progress = 1.0;

            return Root;
        }

        private void Deserialize_TEXT(object reference, Type referenceType, long count, ref byte[] data, ref int offset, IDeserializedObject? nextDeserialized)
        {
            DeserializeCollection_TEXT(reference, referenceType, count, ref data, ref offset);

            Type DeserializedType = SerializableAncestor(referenceType)!;
            List<DeserializedMember> DeserializedMembers = ListDeserializedMembers_TEXT(DeserializedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in DeserializedMembers)
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

                if (Member.MemberInfo is FieldInfo AsFieldInfoBefore)
                    MemberType = AsFieldInfoBefore.FieldType;
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                    MemberType = AsPropertyInfo.PropertyType;
                }

                ProcessDeserializable_TEXT(MemberType, ref data, ref offset, out object? MemberValue);

                if (Member.MemberInfo is FieldInfo AsFieldInfoAfter)
                    AsFieldInfoAfter.SetValue(reference, MemberValue);
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                    if (Member.PropertySetter is null)
                        AsPropertyInfo.SetValue(reference, MemberValue);
                    else
                        Member.PropertySetter.Invoke(reference, new object?[] { MemberValue });
                }
            }

            if (nextDeserialized is not null)
                nextDeserialized.SetDeserialized();
        }

        private void DeserializeCollection_TEXT(object reference, Type referenceType, long count, ref byte[] data, ref int offset)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(reference, referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (i > 0)
                            ReadSeparator_TEXT(ref data, ref offset);

                        if (!ProcessDeserializable_TEXT(ItemType, ref data, ref offset, out object? Item))
                            break;

                        Inserter.AddItem(Item);
                    }
                }
            }
        }

        private delegate object? ReadFieldHandler_TEXT(ref byte[] data, ref int offset);
        private IDictionary<string, ReadFieldHandler_TEXT> ReadFieldHandlerTable_TEXT { get; set; } = new Dictionary<string, ReadFieldHandler_TEXT>();

        private void IniReadFieldHandlerTable_TEXT()
        {
            if (ReadFieldHandlerTable_TEXT.Count == 0)
                ReadFieldHandlerTable_TEXT = new Dictionary<string, ReadFieldHandler_TEXT>()
                {
                    { nameof(SByte), ReadFieldSByte_TEXT },
                    { nameof(Byte), ReadFieldByte_TEXT },
                    { nameof(Boolean), ReadFieldBool_TEXT },
                    { nameof(Char), ReadFieldChar_TEXT },
                    { nameof(Decimal), ReadFieldDecimal_TEXT },
                    { nameof(Double), ReadFieldDouble_TEXT },
                    { nameof(Single), ReadFieldFloat_TEXT },
                    { nameof(Int32), ReadFieldInt_TEXT },
                    { nameof(Int64), ReadFieldLong_TEXT },
                    { nameof(Int16), ReadFieldShort_TEXT },
                    { nameof(UInt32), ReadFieldUInt_TEXT },
                    { nameof(UInt64), ReadFieldULong_TEXT },
                    { nameof(UInt16), ReadFieldUShort_TEXT },
                    { nameof(String), ReadFieldString_TEXT },
                    { nameof(Guid), ReadFieldGuid_TEXT },
                };
        }

        private bool DeserializeBasicType_TEXT(Type? valueType, ref byte[] data, ref int offset, out object? value)
        {
            IniReadFieldHandlerTable_TEXT();

            string? ValueName = valueType?.Name;

            if (ValueName is not null && ReadFieldHandlerTable_TEXT.ContainsKey(ValueName))
            {
                value = ReadFieldHandlerTable_TEXT[ValueName](ref data, ref offset);
                return true;
            }
            else if (valueType is not null && valueType.IsEnum)
            {
                DeserializeEnumType_TEXT(valueType, ref data, ref offset, out value);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private void DeserializeEnumType_TEXT(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    value = Enum.ToObject(valueType, ReadFieldSByte_TEXT(ref data, ref offset));
                    break;
                case nameof(Byte):
                    value = Enum.ToObject(valueType, ReadFieldByte_TEXT(ref data, ref offset));
                    break;
                case nameof(Int16):
                    value = Enum.ToObject(valueType, ReadFieldShort_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt16):
                    value = Enum.ToObject(valueType, ReadFieldUShort_TEXT(ref data, ref offset));
                    break;
                default:
                case nameof(Int32):
                    value = Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt32):
                    value = Enum.ToObject(valueType, ReadFieldUInt_TEXT(ref data, ref offset));
                    break;
                case nameof(Int64):
                    value = Enum.ToObject(valueType, ReadFieldLong_TEXT(ref data, ref offset));
                    break;
                case nameof(UInt64):
                    value = Enum.ToObject(valueType, ReadFieldULong_TEXT(ref data, ref offset));
                    break;
            }
        }

        private bool ProcessDeserializable_TEXT(Type? referenceType, ref byte[] data, ref int offset, out object? reference)
        {
            if (DeserializeBasicType_TEXT(referenceType, ref data, ref offset, out reference))
                return true;

            if (!ReadFieldType_TEXT(ref data, ref offset, out string? ReferenceTypeName) || ReferenceTypeName is null)
            {
                reference = null;
                return false;
            }

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
            {
                reference = null;
                return false;
            }

            Type OriginalType = ReferenceType;
            OverrideType(ref ReferenceType);
            Type NewType = ReferenceType;
            ReferenceType = OriginalType;

            if (ReferenceType.IsValueType)
                return ProcessDeserializableValue_TEXT(NewType, ReferenceType, ref data, ref offset, out reference);
            else
                return ProcessDeserializableReference_TEXT(NewType, ReferenceType, ref data, ref offset, out reference);
        }

        private bool ProcessDeserializableValue_TEXT(Type objectType, Type referenceType, ref byte[] data, ref int offset, out object? reference)
        {
            CreateObject(objectType, out reference);
            Deserialize_TEXT(reference, referenceType, -1, ref data, ref offset, null);

            return true;
        }

        private bool ProcessDeserializableReference_TEXT(Type objectType, Type referenceType, ref byte[] data, ref int offset, out object? reference)
        {
            ObjectTag ReferenceTag = ReadFieldTag_TEXT(ref data, ref offset);

            if (ReferenceTag == ObjectTag.ObjectIndex)
            {
                int ReferenceIndex = ReadFieldObjectIndex_TEXT(ref data, ref offset);
                reference = DeserializedObjectList[ReferenceIndex].Reference;
            }
            else if (ReferenceTag == ObjectTag.ObjectList)
            {
                long Count = ReadFieldCount_TEXT(ref data, ref offset);

                CreateObject(objectType, Count, out reference);
                AddDeserializedObject(reference, referenceType, Count);
            }
            else if (ReferenceTag == ObjectTag.ConstructedObject)
            {
                bool HasSerializableConstructor = ListConstructorParameters(referenceType, out List<SerializedMember> ConstructorParameters);
                Debug.Assert(HasSerializableConstructor);

                object?[] Parameters = new object?[ConstructorParameters.Count];

                for (int i = 0; i < ConstructorParameters.Count; i++)
                {
                    if (i > 0)
                        ReadSeparator_TEXT(ref data, ref offset);

                    PropertyInfo AsPropertyInfo = (PropertyInfo)ConstructorParameters[i].MemberInfo;

                    Type MemberType = AsPropertyInfo.PropertyType;
                    if (!ProcessDeserializable_TEXT(MemberType, ref data, ref offset, out object? MemberValue))
                    {
                        reference = null;
                        return false;
                    }

                    Parameters[i] = MemberValue;
                }

                ReadSeparator_TEXT(ref data, ref offset);

                CreateObject(objectType, Parameters, out reference);
                AddDeserializedObject(reference, referenceType, -1);
            }
            else
            {
                CreateObject(objectType, out reference);
                AddDeserializedObject(reference, referenceType, -1);
            }

            return true;
        }

        private List<DeserializedMember> ListDeserializedMembers_TEXT(Type deserializedType, ref byte[] data, ref int offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                List<string> MemberNames = ReadFieldMembers_TEXT(ref data, ref offset);
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
