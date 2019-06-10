namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

            DeserializedObjectList.Clear();

            object Reference;
            ProcessDeserializable_TEXT(RootType, ref data, ref offset, out Reference);

            Root = Reference;

            if (RootType == null)
                RootType = Root.GetType();

            int i = 0;
            while (i < DeserializedObjectList.Count)
            {
                Progress = i / (double)DeserializedObjectList.Count;

                IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
                Reference = NextDeserialized.Reference;
                Deserialize_TEXT(ref Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref data, ref offset, NextDeserialized);
            }

            Progress = 1.0;

            return Root;
        }

        private void Deserialize_TEXT(ref object reference, Type referenceType, long count, ref byte[] data, ref int offset, IDeserializedObject nextDeserialized)
        {
            DeserializeCollection_TEXT(ref reference, referenceType, count, ref data, ref offset);

            Type DeserializedType = SerializableAncestor(referenceType);
            List<DeserializedMember> DeserializedMembers = ListDeserializedMembers_TEXT(DeserializedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in DeserializedMembers)
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

                ProcessDeserializable_TEXT(MemberType, ref data, ref offset, out MemberValue);

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

        private void DeserializeCollection_TEXT(ref object reference, Type referenceType, long count, ref byte[] data, ref int offset)
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

                        object Item;
                        ProcessDeserializable_TEXT(ItemType, ref data, ref offset, out Item);

                        Inserter.AddItem(Item);
                    }
                }
            }
        }

        private bool DeserializeBasicType_TEXT(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            switch (valueType?.Name)
            {
                case nameof(SByte):
                    value = ReadFieldSByte_TEXT(ref data, ref offset);
                    break;
                case nameof(Byte):
                    value = ReadFieldByte_TEXT(ref data, ref offset);
                    break;
                case nameof(Boolean):
                    value = ReadFieldBool_TEXT(ref data, ref offset);
                    break;
                case nameof(Char):
                    value = ReadFieldChar_TEXT(ref data, ref offset);
                    break;
                case nameof(Decimal):
                    value = ReadFieldDecimal_TEXT(ref data, ref offset);
                    break;
                case nameof(Double):
                    value = ReadFieldDouble_TEXT(ref data, ref offset);
                    break;
                case nameof(Single):
                    value = ReadFieldFloat_TEXT(ref data, ref offset);
                    break;
                case nameof(Int32):
                    value = ReadFieldInt_TEXT(ref data, ref offset);
                    break;
                case nameof(Int64):
                    value = ReadFieldLong_TEXT(ref data, ref offset);
                    break;
                case nameof(Int16):
                    value = ReadFieldShort_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt32):
                    value = ReadFieldUInt_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt64):
                    value = ReadFieldULong_TEXT(ref data, ref offset);
                    break;
                case nameof(UInt16):
                    value = ReadFieldUShort_TEXT(ref data, ref offset);
                    break;
                case nameof(String):
                    value = ReadFieldString_TEXT(ref data, ref offset);
                    break;
                case nameof(Guid):
                    value = ReadFieldGuid_TEXT(ref data, ref offset);
                    break;
                default:
                    if (valueType != null && valueType.IsEnum)
                        DeserializeEnumType_TEXT(valueType, ref data, ref offset, out value);
                    else
                    {
                        value = null;
                        return false;
                    }
                    break;
            }

            return true;
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
                default:
                    value = Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
                    break;
            }
        }

        private void ProcessDeserializable_TEXT(Type referenceType, ref byte[] data, ref int offset, out object reference)
        {
            if (DeserializeBasicType_TEXT(referenceType, ref data, ref offset, out reference))
                return;

            string ReferenceTypeName = ReadFieldType_TEXT(ref data, ref offset);
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
                Deserialize_TEXT(ref reference, referenceType, -1, ref data, ref offset, null);
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_TEXT(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    int ReferenceIndex = ReadFieldObjectIndex_TEXT(ref data, ref offset);
                    reference = DeserializedObjectList[ReferenceIndex].Reference;
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    CreateObject(NewType, ref reference);
                    AddDeserializedObject(reference, referenceType, -1);
                }
                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount_TEXT(ref data, ref offset);

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
                            if (i > 0)
                                ReadSeparator_TEXT(ref data, ref offset);

                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            object MemberValue;
                            Type MemberType = AsPropertyInfo.PropertyType;
                            ProcessDeserializable_TEXT(MemberType, ref data, ref offset, out MemberValue);

                            Parameters[i] = MemberValue;
                        }

                        ReadSeparator_TEXT(ref data, ref offset);

                        CreateObject(NewType, Parameters, ref reference);
                        AddDeserializedObject(reference, referenceType, -1);
                    }
                }
            }
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

        private sbyte ReadFieldSByte_TEXT(ref byte[] data, ref int offset)
        {
            sbyte Value;

            ReadField(ref data, ref offset, 4);

            uint n = 0;
            for (int i = 0; i < 2; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = (sbyte)n;
            offset += 4;

            return Value;
        }

        private byte ReadFieldByte_TEXT(ref byte[] data, ref int offset)
        {
            byte Value;

            ReadField(ref data, ref offset, 4);

            uint n = 0;
            for (int i = 0; i < 2; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = (byte)n;
            offset += 4;

            return Value;
        }

        private bool ReadFieldBool_TEXT(ref byte[] data, ref int offset)
        {
            bool Value;

            ReadField(ref data, ref offset, 4);

            Value = data[offset + 0] == 'T' && data[offset + 1] == 'r' && data[offset + 2] == 'u' && data[offset + 3] == 'e';
            offset += 4;

            if (!Value)
            {
                ReadField(ref data, ref offset, 1);
                offset++;
            }

            return Value;
        }

        private char ReadFieldChar_TEXT(ref byte[] data, ref int offset)
        {
            char Value;

            ReadField(ref data, ref offset, 2);
            int CharOffset = offset;
            offset += 2;

            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != '\'');

            if (offset == CharOffset + 4 && data[CharOffset + 1] == '\\' && data[CharOffset + 2] == '\'')
                Value = '\'';
            else
                Value = Encoding.UTF8.GetString(data, CharOffset + 1, offset - CharOffset - 2)[0];

            return Value;
        }

        private decimal ReadFieldDecimal_TEXT(ref byte[] data, ref int offset)
        {
            decimal Value;

            int BaseOffset = offset;
            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != 'm');

            string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Parsed))
                Value = Parsed;
            else
                Value = default(decimal);

            return Value;
        }

        private double ReadFieldDouble_TEXT(ref byte[] data, ref int offset)
        {
            double Value;

            int BaseOffset = offset;
            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != 'd');

            string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out double Parsed))
                Value = Parsed;
            else
                Value = default(double);

            return Value;
        }

        private float ReadFieldFloat_TEXT(ref byte[] data, ref int offset)
        {
            float Value;

            int BaseOffset = offset;
            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != 'f');

            string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            if (float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out float Parsed))
                Value = Parsed;
            else
                Value = default(float);

            return Value;
        }

        private int ReadFieldInt_TEXT(ref byte[] data, ref int offset)
        {
            int Value;

            ReadField(ref data, ref offset, 10);

            uint n = 0;
            for (int i = 0; i < 8; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = (int)n;
            offset += 10;

            return Value;
        }

        private long ReadFieldLong_TEXT(ref byte[] data, ref int offset)
        {
            long Value;

            ReadField(ref data, ref offset, 18);

            ulong n = 0;
            for (int i = 0; i < 16; i++)
                n = (n * 16) + (ulong)FromHexDigit(data, offset + 2 + i);

            Value = (long)n;
            offset += 18;

            return Value;
        }

        private short ReadFieldShort_TEXT(ref byte[] data, ref int offset)
        {
            short Value;

            ReadField(ref data, ref offset, 6);

            uint n = 0;
            for (int i = 0; i < 4; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = (short)n;
            offset += 6;

            return Value;
        }

        private uint ReadFieldUInt_TEXT(ref byte[] data, ref int offset)
        {
            uint Value;

            ReadField(ref data, ref offset, 10);

            uint n = 0;
            for (int i = 0; i < 8; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = n;
            offset += 10;

            return Value;
        }

        private ulong ReadFieldULong_TEXT(ref byte[] data, ref int offset)
        {
            ulong Value;

            ReadField(ref data, ref offset, 18);

            ulong n = 0;
            for (int i = 0; i < 16; i++)
                n = (n * 16) + (ulong)FromHexDigit(data, offset + 2 + i);

            Value = n;
            offset += 18;

            return Value;
        }

        private ushort ReadFieldUShort_TEXT(ref byte[] data, ref int offset)
        {
            ushort Value;

            ReadField(ref data, ref offset, 6);

            uint n = 0;
            for (int i = 0; i < 4; i++)
                n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

            Value = (ushort)n;
            offset += 6;

            return Value;
        }

        private string ReadFieldString_TEXT(ref byte[] data, ref int offset)
        {
            string Value;

            ReadField(ref data, ref offset, 1);
            if (data[offset] == 'n')
            {
                offset++;
                ReadField(ref data, ref offset, 3);
                if (data[offset + 0] == 'u' && data[offset + 1] == 'l' && data[offset + 2] == 'l')
                    offset += 3;

                return null;
            }

            if (data[offset] != '"')
            {
                offset++;
                return null;
            }

            int BaseOffset = offset++;

            for (;;)
            {
                ReadField(ref data, ref offset, 1);
                if (data[offset] == '\\')
                {
                    offset++;
                    ReadField(ref data, ref offset, 1);
                }
                else if (data[offset] == '"')
                {
                    offset++;
                    break;
                }

                offset++;
            }

            string Content = Encoding.UTF8.GetString(data, BaseOffset + 1, offset - BaseOffset - 2);
            Value = Content.Replace("\\\"", "\"");

            return Value;
        }

        private Guid ReadFieldGuid_TEXT(ref byte[] data, ref int offset)
        {
            Guid Value;

            ReadField(ref data, ref offset, 38);
            string Content = Encoding.UTF8.GetString(data, offset, 38);
            offset += 38;

            if (Guid.TryParse(Content, out Guid AsGuid))
                Value = AsGuid;
            else
                Value = Guid.Empty;

            return Value;
        }

        private string ReadFieldType_TEXT(ref byte[] data, ref int offset)
        {
            string Value;

            int BaseOffset = offset;

            ReadField(ref data, ref offset, 1);
            if (data[offset] != '{')
            {
                if (data[offset++] == 'n')
                {
                    ReadField(ref data, ref offset, 3);
                    if (data[offset + 0] == 'u' && data[offset + 1] == 'l' && data[offset + 2] == 'l')
                        offset += 3;
                }

                return null;
            }

            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != '}');

            Value = Encoding.UTF8.GetString(data, BaseOffset + 1, offset - BaseOffset - 2);

            return Value;
        }

        private List<string> ReadFieldMembers_TEXT(ref byte[] data, ref int offset)
        {
            List<string> MemberNames;

            int BaseOffset = offset;

            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != '\n');

            string AllNames = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            if (AllNames.Length > 0)
            {
                string[] Splitted = AllNames.Split(',');
                MemberNames = new List<string>(Splitted);
            }
            else
                MemberNames = new List<string>();

            return MemberNames;
        }

        private ObjectTag ReadFieldTag_TEXT(ref byte[] data, ref int offset)
        {
            ObjectTag Value;

            ReadField(ref data, ref offset, 1);
            char c = (char)data[offset++];
            if (c == '\n')
                Value = ObjectTag.ObjectReference;
            else if (c == ' ')
            {
                ReadField(ref data, ref offset, 1);
                c = (char)data[offset++];

                if (c == '#')
                    Value = ObjectTag.ObjectIndex;
                else if (c == '!')
                    Value = ObjectTag.ConstructedObject;
                else if (c == '*')
                    Value = ObjectTag.ObjectList;
                else
                    Value = ObjectTag.ObjectReference;
            }
            else
                Value = ObjectTag.ObjectReference;

            return Value;
        }

        private int ReadFieldObjectIndex_TEXT(ref byte[] data, ref int offset)
        {
            int Value;

            int BaseOffset = offset;
            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != '\n');

            int n = 0;
            for (int i = BaseOffset; i + 1 < offset; i++)
                n = (n * 10) + FromDecimalDigit(data, i);

            Value = n;

            return Value;
        }

        private long ReadFieldCount_TEXT(ref byte[] data, ref int offset)
        {
            long Value;

            int BaseOffset = offset;
            do
                ReadField(ref data, ref offset, 1);
            while (data[offset++] != '\n');

            long n = 0;
            for (int i = BaseOffset; i + 1 < offset; i++)
                n = (n * 10) + FromDecimalDigit(data, i);

            Value = n;

            return Value;
        }

        private void ReadSeparator_TEXT(ref byte[] data, ref int offset)
        {
            ReadField(ref data, ref offset, 1);
            char c = (char)data[offset];
            offset++;
        }
    }
}
