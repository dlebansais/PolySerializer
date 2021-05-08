namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private void INTERNAL_Serialize_TEXT()
        {
            RootType = Root?.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            AddFieldStringDirect_TEXT(ref Data, ref Offset, $"Mode={Mode}\n");

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable_TEXT(Root!, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                Progress = i / (double)SerializedObjectList.Count;

                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize_TEXT(Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            Output?.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;

            Progress = 1.0;
        }

        private void Serialize_TEXT(object reference, Type serializedType, long count, ref byte[] data, ref int offset, ISerializableObject? nextSerialized)
        {
            if (count >= 0)
            {
                IEnumerable AsEnumerable = (IEnumerable)reference;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < count; i++)
                {
                    if (i > 0)
                        AddFieldStringDirect_TEXT(ref data, ref offset, ";");

                    Enumerator.MoveNext();

                    object Item = Enumerator.Current!;
                    ProcessSerializable_TEXT(Item, ref data, ref offset);
                }
            }

            List<SerializedMember> SerializedMembers = ListSerializedMembers_TEXT(reference, serializedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (SerializedMember Member in SerializedMembers)
            {
                if (MemberIndex++ > 0)
                    AddFieldStringDirect_TEXT(ref data, ref offset, ";");

                if (Member.Condition.HasValue)
                {
                    AddFieldBool_TEXT(ref data, ref offset, Member.Condition.Value);
                    if (!Member.Condition.Value)
                        continue;

                    AddFieldStringDirect_TEXT(ref data, ref offset, " ");
                }

                object MemberValue;

                if (Member.MemberInfo is FieldInfo AsFieldInfo)
                    MemberValue = AsFieldInfo.GetValue(reference)!;
                else
                {
                    PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                    MemberValue = AsPropertyInfo.GetValue(reference)!;
                }

                ProcessSerializable_TEXT(MemberValue, ref data, ref offset);
            }

            if (nextSerialized != null)
                nextSerialized.SetSerialized();
        }

        private bool SerializeBasicType_TEXT(object value, ref byte[] data, ref int offset)
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
                    return SerializeBasicType1_TEXT(value, ref data, ref offset);
                case sbyte As_sbyte:
                case byte As_byte:
                case int As_int:
                case long As_long:
                case short As_short:
                case uint As_uint:
                case ulong As_ulong:
                case ushort As_ushort:
                    return SerializeBasicType2_TEXT(value, ref data, ref offset);

                default:
                    Type ValueType = value.GetType();

                    if (ValueType.IsEnum)
                        SerializeEnumType_TEXT(value, ValueType, ref data, ref offset);
                    else
                        return false;
                    break;
            }

            return true;
        }

        private bool SerializeBasicType1_TEXT(object value, ref byte[] data, ref int offset)
        {
            switch (value)
            {
                default:
                case bool _:
                    AddFieldBool_TEXT(ref data, ref offset, (bool)value);
                    break;
                case char _:
                    AddFieldChar_TEXT(ref data, ref offset, (char)value);
                    break;
                case decimal _:
                    AddFieldDecimal_TEXT(ref data, ref offset, (decimal)value);
                    break;
                case double _:
                    AddFieldDouble_TEXT(ref data, ref offset, (double)value);
                    break;
                case float _:
                    AddFieldFloat_TEXT(ref data, ref offset, (float)value);
                    break;
                case string _:
                    AddFieldString_TEXT(ref data, ref offset, (string)value);
                    break;
                case Guid _:
                    AddFieldGuid_TEXT(ref data, ref offset, (Guid)value);
                    break;
            }

            return true;
        }

        private bool SerializeBasicType2_TEXT(object value, ref byte[] data, ref int offset)
        {
            switch (value)
            {
                default:
                case sbyte _:
                    AddFieldSByte_TEXT(ref data, ref offset, (sbyte)value);
                    break;
                case byte _:
                    AddFieldByte_TEXT(ref data, ref offset, (byte)value);
                    break;
                case int _:
                    AddFieldInt_TEXT(ref data, ref offset, (int)value);
                    break;
                case long _:
                    AddFieldLong_TEXT(ref data, ref offset, (long)value);
                    break;
                case short _:
                    AddFieldShort_TEXT(ref data, ref offset, (short)value);
                    break;
                case uint _:
                    AddFieldUInt_TEXT(ref data, ref offset, (uint)value);
                    break;
                case ulong _:
                    AddFieldULong_TEXT(ref data, ref offset, (ulong)value);
                    break;
                case ushort _:
                    AddFieldUShort_TEXT(ref data, ref offset, (ushort)value);
                    break;
            }

            return true;
        }

        private void SerializeEnumType_TEXT(object value, Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            switch (UnderlyingSystemType.Name)
            {
                case nameof(SByte):
                    AddFieldSByte_TEXT(ref data, ref offset, (sbyte)value);
                    break;
                case nameof(Byte):
                    AddFieldByte_TEXT(ref data, ref offset, (byte)value);
                    break;
                case nameof(Int16):
                    AddFieldShort_TEXT(ref data, ref offset, (short)value);
                    break;
                case nameof(UInt16):
                    AddFieldUShort_TEXT(ref data, ref offset, (ushort)value);
                    break;
                default:
                case nameof(Int32):
                    AddFieldInt_TEXT(ref data, ref offset, (int)value);
                    break;
                case nameof(UInt32):
                    AddFieldUInt_TEXT(ref data, ref offset, (uint)value);
                    break;
                case nameof(Int64):
                    AddFieldLong_TEXT(ref data, ref offset, (long)value);
                    break;
                case nameof(UInt64):
                    AddFieldULong_TEXT(ref data, ref offset, (ulong)value);
                    break;
            }
        }

        private void ProcessSerializable_TEXT(object reference, ref byte[] data, ref int offset)
        {
            if (reference == null)
            {
                AddFieldNull_TEXT(ref data, ref offset);
                return;
            }

            if (SerializeBasicType_TEXT(reference, ref data, ref offset))
                return;

            Type ReferenceType = SerializableAncestor(reference.GetType())!;
            AddFieldType_TEXT(ref data, ref offset, ReferenceType);

            if (ReferenceType.IsValueType)
                Serialize_TEXT(reference, ReferenceType, -1, ref data, ref offset, null);
            else
            {
                if (CycleDetectionTable.ContainsKey(reference))
                {
                    long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[reference]);

                    AddFieldStringDirect_TEXT(ref data, ref offset, $" #{ReferenceIndex}\n");
                }
                else
                {
                    long Count = GetCollectionCount(reference);
                    if (Count < 0)
                    {
                        List<SerializedMember> ConstructorParameters;
                        if (ListConstructorParameters(ReferenceType, out ConstructorParameters))
                        {
                            AddFieldStringDirect_TEXT(ref data, ref offset, " !");

                            int ParameterIndex = 0;
                            foreach (SerializedMember Member in ConstructorParameters)
                            {
                                if (ParameterIndex++ > 0)
                                    AddFieldStringDirect_TEXT(ref data, ref offset, ";");

                                PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                                object MemberValue = AsPropertyInfo.GetValue(reference)!;

                                ProcessSerializable_TEXT(MemberValue, ref data, ref offset);
                            }

                            AddFieldStringDirect_TEXT(ref data, ref offset, "\n");
                        }
                        else
                            AddFieldStringDirect_TEXT(ref data, ref offset, "\n");
                    }
                    else
                        AddFieldStringDirect_TEXT(ref data, ref offset, $" *{Count}\n");

                    AddSerializedObject(reference, Count);
                }
            }
        }

        private void AddFieldSByte_TEXT(ref byte[] data, ref int offset, sbyte value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{((byte)value).ToString("X02", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldByte_TEXT(ref byte[] data, ref int offset, byte value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X02", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldBool_TEXT(ref byte[] data, ref int offset, bool value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"{value}");
        }

        private void AddFieldChar_TEXT(ref byte[] data, ref int offset, char value)
        {
            if (value == '\'')
                AddFieldStringDirect_TEXT(ref data, ref offset, @"'\''");
            else
                AddFieldStringDirect_TEXT(ref data, ref offset, $"'{value}'");
        }

        private void AddFieldDecimal_TEXT(ref byte[] data, ref int offset, decimal value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}m");
        }

        private void AddFieldDouble_TEXT(ref byte[] data, ref int offset, double value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}d");
        }

        private void AddFieldFloat_TEXT(ref byte[] data, ref int offset, float value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}f");
        }

        private void AddFieldInt_TEXT(ref byte[] data, ref int offset, int value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X08", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldLong_TEXT(ref byte[] data, ref int offset, long value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X16", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldShort_TEXT(ref byte[] data, ref int offset, short value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X04", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldUInt_TEXT(ref byte[] data, ref int offset, uint value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X08", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldULong_TEXT(ref byte[] data, ref int offset, ulong value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X16", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldUShort_TEXT(ref byte[] data, ref int offset, ushort value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X04", CultureInfo.InvariantCulture)}");
        }

        private void AddFieldString_TEXT(ref byte[] data, ref int offset, string value)
        {
            value = "\"" + value.Replace("\"", "\\\"") + "\"";
            AddField(ref data, ref offset, Encoding.UTF8.GetBytes(value));
        }

        private void AddFieldGuid_TEXT(ref byte[] data, ref int offset, Guid value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, value.ToString("B"));
        }

        private void AddFieldNull_TEXT(ref byte[] data, ref int offset)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, "null");
        }

        private void AddFieldType_TEXT(ref byte[] data, ref int offset, Type value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"{{{value.AssemblyQualifiedName}}}");
        }

        private void AddFieldStringDirect_TEXT(ref byte[] data, ref int offset, string s)
        {
            AddField(ref data, ref offset, Encoding.UTF8.GetBytes(s));
        }

        private void AddFieldMembers_TEXT(ref byte[] data, ref int offset, List<SerializedMember> serializedMembers)
        {
            for (int i = 0; i < serializedMembers.Count; i++)
            {
                if (i > 0)
                    AddFieldStringDirect_TEXT(ref data, ref offset, ",");

                SerializedMember Member = serializedMembers[i];
                AddFieldStringDirect_TEXT(ref data, ref offset, Member.MemberInfo.Name);
            }

            AddFieldStringDirect_TEXT(ref data, ref offset, "\n");
        }

        private List<SerializedMember> ListSerializedMembers_TEXT(object reference, Type serializedType, ref byte[] data, ref int offset)
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
                AddFieldMembers_TEXT(ref data, ref offset, SerializedMembers);

            return SerializedMembers;
        }
    }
}
