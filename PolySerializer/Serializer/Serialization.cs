namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        /// <summary>
        ///     Serializes <paramref name="root"/> and write the serialized data to <paramref name="output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// </parameters>
        public void Serialize(Stream output, object root)
        {
            InitializeSerialization(output, root);
            INTERNAL_Serialize();
        }

        /// <summary>
        ///     Serializes <paramref name="root"/> and write the serialized data to <paramref name="output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// </parameters>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        public Task SerializeAsync(Stream output, object root)
        {
            InitializeSerialization(output, root);
            return Task.Run(() => INTERNAL_Serialize());
        }

        private void InitializeSerialization(Stream output, object root)
        {
            Output = output;
            Root = root;
            Progress = 0;
        }

        private void AddSerializedObject(object reference, long count)
        {
            Type SerializedType = SerializableAncestor(reference.GetType());
            SerializableObject NewSerialized = new SerializableObject(reference, SerializedType, count);
            SerializedObjectList.Add(NewSerialized);

            CycleDetectionTable.Add(reference, NewSerialized);
        }

        private long GetCollectionCount(object reference)
        {
            IEnumerable AsEnumerable;

            long Count = 0;
            if ((AsEnumerable = reference as IEnumerable) != null)
            {
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();
                while (Enumerator.MoveNext())
                    Count++;

                return Count;
            }
            else
                return -1;
        }

        private void AddField(ref byte[] data, ref int offset, byte[] content)
        {
            if (offset + content.Length > data.Length)
            {
                Output.Write(data, 0, offset);
                offset = 0;

                if (data.Length < content.Length)
                    data = new byte[content.Length];
            }

            for (int i = 0; i < content.Length; i++)
                data[offset++] = content[i];
        }

        private void INTERNAL_Serialize()
        {
            bool IsSerializedAsText = (Format == SerializationFormat.TextPreferred) || (Format == SerializationFormat.TextOnly);

            if (IsSerializedAsText)
                INTERNAL_Serialize_TEXT();
            else
                INTERNAL_Serialize_BINARY();
        }

        #region Binary
        private void INTERNAL_Serialize_BINARY()
        {
            RootType = Root.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            AddFieldInt_BINARY(ref Data, ref Offset, (int)Mode);

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable_BINARY(Root, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                Progress = i / (double)SerializedObjectList.Count;

                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize_BINARY(Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            Output.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;

            Progress = 1.0;
        }

        private void Serialize_BINARY(object reference, Type serializedType, long count, ref byte[] data, ref int offset, ISerializableObject nextSerialized)
        {
            if (count >= 0)
            {
                IEnumerable AsEnumerable = reference as IEnumerable;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < count; i++)
                {
                    Enumerator.MoveNext();

                    object Item = Enumerator.Current;
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

                FieldInfo AsFieldInfo;
                PropertyInfo AsPropertyInfo;

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    MemberValue = AsFieldInfo.GetValue(reference);
                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    MemberValue = AsPropertyInfo.GetValue(reference);
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
                case sbyte As_sbyte:
                    AddFieldSByte_BINARY(ref data, ref offset, (sbyte)value);
                    break;
                case byte As_byte:
                    AddFieldByte_BINARY(ref data, ref offset, (byte)value);
                    break;
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
                case string As_string:
                    AddFieldString_BINARY(ref data, ref offset, (string)value);
                    break;
                case Guid As_Guid:
                    AddFieldGuid_BINARY(ref data, ref offset, (Guid)value);
                    break;

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
                default:
                    AddFieldInt_BINARY(ref data, ref offset, (int)value);
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

            Type ReferenceType = SerializableAncestor(reference.GetType());
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
                                PropertyInfo AsPropertyInfo;
                                AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                                object MemberValue = AsPropertyInfo.GetValue(reference);

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
            AddFieldString_BINARY(ref data, ref offset, value.AssemblyQualifiedName);
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
        #endregion

        #region Text
        private void INTERNAL_Serialize_TEXT()
        {
            RootType = Root.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            AddFieldStringDirect_TEXT(ref Data, ref Offset, $"Mode={Mode}\n");

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable_TEXT(Root, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                Progress = i / (double)SerializedObjectList.Count;

                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize_TEXT(Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            Output.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;

            Progress = 1.0;
        }

        private void Serialize_TEXT(object reference, Type serializedType, long count, ref byte[] data, ref int offset, ISerializableObject nextSerialized)
        {
            if (count >= 0)
            {
                IEnumerable AsEnumerable = reference as IEnumerable;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < count; i++)
                {
                    if (i > 0)
                        AddFieldStringDirect_TEXT(ref data, ref offset, ";");

                    Enumerator.MoveNext();

                    object Item = Enumerator.Current;
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

                FieldInfo AsFieldInfo;
                PropertyInfo AsPropertyInfo;

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    MemberValue = AsFieldInfo.GetValue(reference);
                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    MemberValue = AsPropertyInfo.GetValue(reference);
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
                case sbyte As_sbyte:
                    AddFieldSByte_TEXT(ref data, ref offset, (sbyte)value);
                    break;
                case byte As_byte:
                    AddFieldByte_TEXT(ref data, ref offset, (byte)value);
                    break;
                case bool As_bool:
                    AddFieldBool_TEXT(ref data, ref offset, (bool)value);
                    break;
                case char As_char:
                    AddFieldChar_TEXT(ref data, ref offset, (char)value);
                    break;
                case decimal As_decimal:
                    AddFieldDecimal_TEXT(ref data, ref offset, (decimal)value);
                    break;
                case double As_double:
                    AddFieldDouble_TEXT(ref data, ref offset, (double)value);
                    break;
                case float As_float:
                    AddFieldFloat_TEXT(ref data, ref offset, (float)value);
                    break;
                case int As_int:
                    AddFieldInt_TEXT(ref data, ref offset, (int)value);
                    break;
                case long As_long:
                    AddFieldLong_TEXT(ref data, ref offset, (long)value);
                    break;
                case short As_short:
                    AddFieldShort_TEXT(ref data, ref offset, (short)value);
                    break;
                case uint As_uint:
                    AddFieldUInt_TEXT(ref data, ref offset, (uint)value);
                    break;
                case ulong As_ulong:
                    AddFieldULong_TEXT(ref data, ref offset, (ulong)value);
                    break;
                case ushort As_ushort:
                    AddFieldUShort_TEXT(ref data, ref offset, (ushort)value);
                    break;
                case string As_string:
                    AddFieldString_TEXT(ref data, ref offset, (string)value);
                    break;
                case Guid As_Guid:
                    AddFieldGuid_TEXT(ref data, ref offset, (Guid)value);
                    break;

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
                default:
                    AddFieldInt_TEXT(ref data, ref offset, (int)value);
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

            Type ReferenceType = SerializableAncestor(reference.GetType());
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

                                PropertyInfo AsPropertyInfo;
                                AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                                object MemberValue = AsPropertyInfo.GetValue(reference);

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
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{((byte)value).ToString("X02")}");
        }

        private void AddFieldByte_TEXT(ref byte[] data, ref int offset, byte value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X02")}");
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
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X08")}");
        }

        private void AddFieldLong_TEXT(ref byte[] data, ref int offset, long value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X16")}");
        }

        private void AddFieldShort_TEXT(ref byte[] data, ref int offset, short value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X04")}");
        }

        private void AddFieldUInt_TEXT(ref byte[] data, ref int offset, uint value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X08")}");
        }

        private void AddFieldULong_TEXT(ref byte[] data, ref int offset, ulong value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X16")}");
        }

        private void AddFieldUShort_TEXT(ref byte[] data, ref int offset, ushort value)
        {
            AddFieldStringDirect_TEXT(ref data, ref offset, $"0x{value.ToString("X04")}");
        }

        private void AddFieldString_TEXT(ref byte[] data, ref int offset, string value)
        {
            if (value == null)
                value = "null";
            else
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
        #endregion

        private List<ISerializableObject> SerializedObjectList = new List<ISerializableObject>();
        private Dictionary<object, SerializableObject> CycleDetectionTable = new Dictionary<object, SerializableObject>();
    }
}
