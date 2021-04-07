namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private void INTERNAL_Serialize_BINARY()
        {
            RootType = Root?.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            AddFieldInt_BINARY(ref Data, ref Offset, (int)Mode);

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable_BINARY(Root!, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                Progress = i / (double)SerializedObjectList.Count;

                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize_BINARY(Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            Output?.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;

            Progress = 1.0;
        }

        private void Serialize_BINARY(object reference, Type serializedType, long count, ref byte[] data, ref int offset, ISerializableObject? nextSerialized)
        {
            if (count >= 0)
            {
                IEnumerable AsEnumerable = (IEnumerable)reference;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < count; i++)
                {
                    Enumerator.MoveNext();

                    object Item = Enumerator.Current!;
                    ProcessSerializable_BINARY(Item, ref data, ref offset);
                }
            }

            List<SerializedMember> SerializedMembers = ListSerializedMembers_BINARY(reference, serializedType, ref data, ref offset);

            foreach (SerializedMember Member in SerializedMembers)
            {
                if (Member.Condition.HasValue)
                {
                    AddFieldBool_BINARY(ref data, ref offset, Member.Condition.Value);
                    if (!Member.Condition.Value)
                        continue;
                }

                object MemberValue;

                if (Member.MemberInfo is FieldInfo AsFieldInfo)
                    MemberValue = AsFieldInfo.GetValue(reference)!;
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                    MemberValue = AsPropertyInfo.GetValue(reference)!;
                }

                ProcessSerializable_BINARY(MemberValue, ref data, ref offset);
            }

            if (nextSerialized != null)
                nextSerialized.SetSerialized();
        }

        private bool SerializeBasicType_BINARY(object value, ref byte[] data, ref int offset)
        {
            switch (value)
            {
                case bool As_bool:
                case char As_char:
                case decimal As_decimal:
                case double As_double:
                case float As_float:
                case string As_string:
                case Guid As_Guid:
                    return SerializeBasicType1_BINARY(value, ref data, ref offset);

                case sbyte As_sbyte:
                case byte As_byte:
                case int As_int:
                case long As_long:
                case short As_short:
                case uint As_uint:
                case ulong As_ulong:
                case ushort As_ushort:
                    return SerializeBasicType2_BINARY(value, ref data, ref offset);

                default:
                    Type ValueType = value.GetType();

                    if (ValueType.IsEnum)
                        SerializeEnumType_BINARY(value, ValueType, ref data, ref offset);
                    else
                        return false;
                    break;
            }

            return true;
        }

        private bool SerializeBasicType1_BINARY(object value, ref byte[] data, ref int offset)
        {
            switch (value)
            {
                default:
                case bool As_bool:
                    AddFieldBool_BINARY(ref data, ref offset, (bool)value);
                    break;
                case char As_char:
                    AddFieldChar_BINARY(ref data, ref offset, (char)value);
                    break;
                case decimal As_decimal:
                    AddFieldDecimal_BINARY(ref data, ref offset, (decimal)value);
                    break;
                case double As_double:
                    AddFieldDouble_BINARY(ref data, ref offset, (double)value);
                    break;
                case float As_float:
                    AddFieldFloat_BINARY(ref data, ref offset, (float)value);
                    break;
                case string As_string:
                    AddFieldString_BINARY(ref data, ref offset, (string)value);
                    break;
                case Guid As_Guid:
                    AddFieldGuid_BINARY(ref data, ref offset, (Guid)value);
                    break;
            }

            return true;
        }

        private bool SerializeBasicType2_BINARY(object value, ref byte[] data, ref int offset)
        {
            switch (value)
            {
                default:
                case sbyte As_sbyte:
                    AddFieldSByte_BINARY(ref data, ref offset, (sbyte)value);
                    break;
                case byte As_byte:
                    AddFieldByte_BINARY(ref data, ref offset, (byte)value);
                    break;
                case int As_int:
                    AddFieldInt_BINARY(ref data, ref offset, (int)value);
                    break;
                case long As_long:
                    AddFieldLong_BINARY(ref data, ref offset, (long)value);
                    break;
                case short As_short:
                    AddFieldShort_BINARY(ref data, ref offset, (short)value);
                    break;
                case uint As_uint:
                    AddFieldUInt_BINARY(ref data, ref offset, (uint)value);
                    break;
                case ulong As_ulong:
                    AddFieldULong_BINARY(ref data, ref offset, (ulong)value);
                    break;
                case ushort As_ushort:
                    AddFieldUShort_BINARY(ref data, ref offset, (ushort)value);
                    break;
            }

            return true;
        }

        private void SerializeEnumType_BINARY(object value, Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    AddFieldSByte_BINARY(ref data, ref offset, (sbyte)value);
                    break;
                case nameof(Byte):
                    AddFieldByte_BINARY(ref data, ref offset, (byte)value);
                    break;
                case nameof(Int16):
                    AddFieldShort_BINARY(ref data, ref offset, (short)value);
                    break;
                case nameof(UInt16):
                    AddFieldUShort_BINARY(ref data, ref offset, (ushort)value);
                    break;
                default:
                case nameof(Int32):
                    AddFieldInt_BINARY(ref data, ref offset, (int)value);
                    break;
                case nameof(UInt32):
                    AddFieldUInt_BINARY(ref data, ref offset, (uint)value);
                    break;
                case nameof(Int64):
                    AddFieldLong_BINARY(ref data, ref offset, (long)value);
                    break;
                case nameof(UInt64):
                    AddFieldULong_BINARY(ref data, ref offset, (ulong)value);
                    break;
            }
        }

        private void ProcessSerializable_BINARY(object reference, ref byte[] data, ref int offset)
        {
            if (reference == null)
            {
                AddFieldNull_BINARY(ref data, ref offset);
                return;
            }

            if (SerializeBasicType_BINARY(reference, ref data, ref offset))
                return;

            Type ReferenceType = SerializableAncestor(reference.GetType())!;
            AddFieldType_BINARY(ref data, ref offset, ReferenceType);

            if (ReferenceType.IsValueType)
                Serialize_BINARY(reference, ReferenceType, -1, ref data, ref offset, null);
            else
            {
                if (CycleDetectionTable.ContainsKey(reference))
                {
                    long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[reference]);

                    AddFieldByte_BINARY(ref data, ref offset, (byte)ObjectTag.ObjectIndex);
                    AddFieldLong_BINARY(ref data, ref offset, ReferenceIndex);
                }
                else
                {
                    long Count = GetCollectionCount(reference);
                    if (Count < 0)
                    {
                        List<SerializedMember> ConstructorParameters;
                        if (ListConstructorParameters(ReferenceType, out ConstructorParameters))
                        {
                            AddFieldByte_BINARY(ref data, ref offset, (byte)ObjectTag.ConstructedObject);

                            foreach (SerializedMember Member in ConstructorParameters)
                            {
                                PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                                object MemberValue = AsPropertyInfo.GetValue(reference)!;

                                ProcessSerializable_BINARY(MemberValue, ref data, ref offset);
                            }
                        }
                        else
                            AddFieldByte_BINARY(ref data, ref offset, (byte)ObjectTag.ObjectReference);
                    }
                    else
                    {
                        AddFieldByte_BINARY(ref data, ref offset, (byte)ObjectTag.ObjectList);
                        AddFieldLong_BINARY(ref data, ref offset, Count);
                    }

                    AddSerializedObject(reference, Count);
                }
            }
        }

        private void AddFieldSByte_BINARY(ref byte[] data, ref int offset, sbyte value)
        {
            AddField(ref data, ref offset, new byte[1] { (byte)value });
        }

        private void AddFieldByte_BINARY(ref byte[] data, ref int offset, byte value)
        {
            AddField(ref data, ref offset, new byte[1] { value });
        }

        private void AddFieldBool_BINARY(ref byte[] data, ref int offset, bool value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldChar_BINARY(ref byte[] data, ref int offset, char value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldDecimal_BINARY(ref byte[] data, ref int offset, decimal value)
        {
            int[] DecimalInts = decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                byte[] DecimalBytes = BitConverter.GetBytes(DecimalInts[i]);
                AddField(ref data, ref offset, DecimalBytes);
            }
        }

        private void AddFieldDouble_BINARY(ref byte[] data, ref int offset, double value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldFloat_BINARY(ref byte[] data, ref int offset, float value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldInt_BINARY(ref byte[] data, ref int offset, int value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldLong_BINARY(ref byte[] data, ref int offset, long value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldShort_BINARY(ref byte[] data, ref int offset, short value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUInt_BINARY(ref byte[] data, ref int offset, uint value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldULong_BINARY(ref byte[] data, ref int offset, ulong value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUShort_BINARY(ref byte[] data, ref int offset, ushort value)
        {
            AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldString_BINARY(ref byte[] data, ref int offset, string value)
        {
            AddField(ref data, ref offset, String2Bytes(value));
        }

        private void AddFieldGuid_BINARY(ref byte[] data, ref int offset, Guid value)
        {
            AddField(ref data, ref offset, value.ToByteArray());
        }

        private void AddFieldNull_BINARY(ref byte[] data, ref int offset)
        {
            AddField(ref data, ref offset, new byte[CountByteSize] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        }

        private void AddFieldType_BINARY(ref byte[] data, ref int offset, Type value)
        {
            AddFieldString_BINARY(ref data, ref offset, value.AssemblyQualifiedName!);
        }

        private void AddFieldMembers_BINARY(ref byte[] data, ref int offset, List<SerializedMember> serializedMembers)
        {
            AddFieldInt_BINARY(ref data, ref offset, serializedMembers.Count);

            foreach (SerializedMember Member in serializedMembers)
                AddFieldString_BINARY(ref data, ref offset, Member.MemberInfo.Name);
        }

        private List<SerializedMember> ListSerializedMembers_BINARY(object reference, Type serializedType, ref byte[] data, ref int offset)
        {
            List<MemberInfo> Members = new List<MemberInfo>(serializedType.GetMembers());
            List<SerializedMember> SerializedMembers = new List<SerializedMember>();

            foreach (MemberInfo MemberInfo in Members)
            {
                SerializedMember NewMember = new SerializedMember(MemberInfo);

                if (IsSerializableMember(reference, serializedType, NewMember))
                    SerializedMembers.Add(NewMember);
            }

            if (Mode == SerializationMode.Default)
                SerializedMembers.Sort(SortByName);
            else if (Mode == SerializationMode.MemberName)
                AddFieldMembers_BINARY(ref data, ref offset, SerializedMembers);

            return SerializedMembers;
        }
    }
}
