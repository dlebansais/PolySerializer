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
        /// <summary>
        ///     Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        /// <returns>
        ///     The deserialized object.
        /// </returns>
        public object Deserialize(Stream input)
        {
            InitializeDeserialization(input);
            return INTERNAL_Deserialize();
        }

        /// <summary>
        ///     Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        public Task<object> DeserializeAsync(Stream input)
        {
            InitializeDeserialization(input);
            return Task.Run(() => INTERNAL_Deserialize());
        }

        private void InitializeDeserialization(Stream input)
        {
            Input = input;
            Progress = 0;
        }

        private object INTERNAL_Deserialize()
        {
            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            ReadField(ref Data, ref Offset, 4);

            bool IsDeserializedAsText;
            if (Format == SerializationFormat.TextPreferred || Format == SerializationFormat.BinaryPreferred)
                IsDeserializedAsText = Data[0] == 'M' && Data[1] == 'o' && Data[2] == 'd' && Data[3] == 'e';
            else
                IsDeserializedAsText = Format == SerializationFormat.TextOnly;

            if (IsDeserializedAsText)
                return INTERNAL_Deserialize_TEXT(ref Data, ref Offset);
            else
                return INTERNAL_Deserialize_BINARY(ref Data, ref Offset);
        }

        private void ReadStringField(ref byte[] data, ref int offset, out string value)
        {
            ReadField(ref data, ref offset, CountByteSize);
            int CharCount = BitConverter.ToInt32(data, offset);

            offset += CountByteSize;
            if (CharCount < 0)
                value = null;
            else
            {
                ReadField(ref data, ref offset, CharCount * 2);
                value = Bytes2String(CharCount, data, offset);
                offset += CharCount * 2;
            }
        }

        private void CreateObject(Type referenceType, ref object reference)
        {
            try
            {
                reference = Activator.CreateInstance(referenceType);
            }
            catch
            {
            }
        }

        private void CreateObject(Type referenceType, object[] parameters, ref object reference)
        {
            try
            {
                reference = Activator.CreateInstance(referenceType, parameters);
            }
            catch
            {
            }
        }

        private void CreateObject(Type valueType, long count, ref object reference)
        {
            try
            {
                if (valueType.IsArray)
                {
                    Type ArrayType = valueType.GetElementType();
                    reference = Array.CreateInstance(ArrayType, count);
                }
                else if (count < int.MaxValue)
                {
                    reference = Activator.CreateInstance(valueType, (int)count);
                }
                else
                    reference = Activator.CreateInstance(valueType, count);
            }
            catch
            {
            }
        }

        private Type DeserializedTrueType(string typeName)
        {
            return Type.GetType(typeName);
        }

        private bool OverrideTypeName(ref string referenceTypeName)
        {
            if (NamespaceOverrideTable.Count == 0)
                return false;

            TypeIdentifier Identifier = new TypeIdentifier(referenceTypeName);
            if (Identifier.Override(NamespaceOverrideTable, OverrideGenericArguments))
            {
                referenceTypeName = Identifier.Name;
                return true;
            }

            return false;
        }

        private bool OverrideType(ref Type referenceType)
        {
            if (TypeOverrideTable.Count == 0 && AssemblyOverrideTable.Count == 0)
                return false;

            if (TypeOverrideTable.Count > 0)
            {
                if (OverrideDirectType(ref referenceType))
                    return true;

                if (OverrideGenericDefinitionType(ref referenceType))
                    return true;
            }

            if (AssemblyOverrideTable.Count > 0)
            {
                bool GlobalOverride = false;

                DeconstructType(referenceType, out Type[] TypeList);

                for (int i = 0; i < TypeList.Length; i++)
                {
                    if (!OverrideGenericArguments && i > 0)
                        break;

                    Type Type = TypeList[i];

                    Assembly Assembly = Type.Assembly;
                    if (AssemblyOverrideTable.ContainsKey(Assembly))
                    {
                        Assembly = AssemblyOverrideTable[Assembly];

                        GlobalOverride = true;
                        Type = Assembly.GetType(Type.FullName);
                        if (Type != null)
                            TypeList[i] = Type;
                    }
                }

                if (GlobalOverride)
                {
                    ReconstructType(TypeList, out referenceType);
                    return true;
                }
            }

            return false;
        }

        private void DeconstructType(Type type, out Type[] typeList)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type[] GenericArguments = type.GetGenericArguments();
                typeList = new Type[1 + GenericArguments.Length];
                typeList[0] = type.GetGenericTypeDefinition();
                for (int i = 0; i < GenericArguments.Length; i++)
                    typeList[i + 1] = GenericArguments[i];
            }
            else
            {
                typeList = new Type[1];
                typeList[0] = type;
            }
        }

        private void ReconstructType(Type[] typeList, out Type type)
        {
            if (typeList.Length == 1)
                type = typeList[0];
            else
            {
                Type[] GenericArguments = new Type[typeList.Length - 1];
                for (int i = 1; i < typeList.Length; i++)
                    GenericArguments[i - 1] = typeList[i];
                type = typeList[0].MakeGenericType(GenericArguments);
            }
        }

        private bool OverrideDirectType(ref Type referenceType)
        {
            if (!TypeOverrideTable.ContainsKey(referenceType))
                return false;

            referenceType = TypeOverrideTable[referenceType];
            return true;
        }

        private bool OverrideGenericDefinitionType(ref Type referenceType)
        {
            if (!referenceType.IsGenericType || referenceType.IsGenericTypeDefinition)
                return false;

            bool Override = false;

            Type GenericTypeDefinition = referenceType.GetGenericTypeDefinition();
            Override |= OverrideType(ref GenericTypeDefinition);

            Type[] GenericArguments = referenceType.GetGenericArguments();
            if (OverrideGenericArguments)
                for (int i = 0; i < GenericArguments.Length; i++)
                    Override |= OverrideType(ref GenericArguments[i]);

            if (Override)
            {
                referenceType = GenericTypeDefinition.MakeGenericType(GenericArguments);
                return true;
            }

            return false;
        }

        private void AddDeserializedObject(object reference, Type deserializedType, long count)
        {
            DeserializedObjectList.Add(new DeserializedObject(reference, deserializedType, count));
        }

        private bool IsDeserializableMember(Type deserializedType, DeserializedMember newMember)
        {
            if (newMember.MemberInfo.MemberType != MemberTypes.Field && newMember.MemberInfo.MemberType != MemberTypes.Property)
                return false;

            if (IsStaticOrReadOnly(newMember.MemberInfo))
                return false;

            if (IsExcludedFromDeserialization(newMember))
                return false;

            if (IsReadOnlyPropertyWithNoValidSetter(deserializedType, newMember))
                return false;

            if (IsExcludedIndexer(newMember))
                return false;

            CheckForSerializedCondition(newMember);

            return true;
        }

        private bool IsExcludedFromDeserialization(DeserializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private bool IsReadOnlyPropertyWithNoValidSetter(Type deserializedType, DeserializedMember newMember)
        {
            PropertyInfo AsPropertyInfo;
            if ((AsPropertyInfo = newMember.MemberInfo as PropertyInfo) != null)
            {
                if (AsPropertyInfo.CanWrite)
                {
                    Debug.Assert(AsPropertyInfo.SetMethod != null);
                    MethodInfo Setter = AsPropertyInfo.SetMethod;
                    if (Setter.Attributes.HasFlag(MethodAttributes.Public))
                        return false;
                }

                SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
                if (CustomSerializable != null)
                {
                    if (CustomSerializable.Setter != null)
                    {
                        MemberInfo[] SetterMembers = deserializedType.GetMember(CustomSerializable.Setter);
                        if (SetterMembers != null)
                        {
                            Type ExpectedParameterType = AsPropertyInfo.PropertyType;

                            foreach (MemberInfo SetterMember in SetterMembers)
                            {
                                MethodInfo AsMethodInfo;

                                if ((AsMethodInfo = SetterMember as MethodInfo) != null)
                                {
                                    ParameterInfo[] Parameters = AsMethodInfo.GetParameters();
                                    if (Parameters != null && Parameters.Length == 1)
                                    {
                                        ParameterInfo Parameter = Parameters[0];
                                        if (Parameter.ParameterType == ExpectedParameterType)
                                        {
                                            newMember.SetPropertySetter(AsMethodInfo);
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
                return false;

            return true;
        }

        private bool IsExcludedIndexer(DeserializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
                return false;

            if (newMember.MemberInfo.Name == "Item" && newMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private void CheckForSerializedCondition(DeserializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Condition != null)
                    newMember.SetHasCondition();
            }
        }

        private void ReadField(ref byte[] data, ref int offset, int minLength)
        {
            bool Reload = false;

            if (offset + minLength > data.Length)
            {
                int i;
                for (i = 0; i < data.Length - offset; i++)
                    data[i] = data[i + offset];
                offset = i;

                Reload = true;
            }
            else if (offset == 0)
                Reload = true;

            if (Reload)
            {
                long Length = Input.Length - Input.Position;
                if (Length > data.Length - offset)
                    Length = data.Length - offset;

                Input.Read(data, offset, (int)Length);
                offset = 0;
            }
        }

        #region Binary
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

        private sbyte ReadFieldSByte_BINARY(ref byte[] data, ref int offset)
        {
            sbyte Value;

            ReadField(ref data, ref offset, 1);
            Value = (sbyte)data[offset];
            offset++;

            return Value;
        }

        private byte ReadFieldByte_BINARY(ref byte[] data, ref int offset)
        {
            byte Value;

            ReadField(ref data, ref offset, 1);
            Value = data[offset];
            offset++;

            return Value;
        }

        private bool ReadFieldBool_BINARY(ref byte[] data, ref int offset)
        {
            bool Value;

            ReadField(ref data, ref offset, 1);
            Value = BitConverter.ToBoolean(data, offset);
            offset += 1;

            return Value;
        }

        private char ReadFieldChar_BINARY(ref byte[] data, ref int offset)
        {
            char Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToChar(data, offset);
            offset += 2;

            return Value;
        }

        private decimal ReadFieldDecimal_BINARY(ref byte[] data, ref int offset)
        {
            decimal Value;

            ReadField(ref data, ref offset, 16);
            int[] DecimalInts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                DecimalInts[i] = BitConverter.ToInt32(data, offset);
                offset += 4;
            }

            Value = new decimal(DecimalInts);

            return Value;
        }

        private double ReadFieldDouble_BINARY(ref byte[] data, ref int offset)
        {
            double Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToDouble(data, offset);
            offset += 8;

            return Value;
        }

        private float ReadFieldFloat_BINARY(ref byte[] data, ref int offset)
        {
            float Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToSingle(data, offset);
            offset += 4;

            return Value;
        }

        private int ReadFieldInt_BINARY(ref byte[] data, ref int offset)
        {
            int Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToInt32(data, offset);
            offset += 4;

            return Value;
        }

        private long ReadFieldLong_BINARY(ref byte[] data, ref int offset)
        {
            long Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToInt64(data, offset);
            offset += 8;

            return Value;
        }

        private short ReadFieldShort_BINARY(ref byte[] data, ref int offset)
        {
            short Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToInt16(data, offset);
            offset += 2;

            return Value;
        }

        private uint ReadFieldUInt_BINARY(ref byte[] data, ref int offset)
        {
            uint Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToUInt32(data, offset);
            offset += 4;

            return Value;
        }

        private ulong ReadFieldULong_BINARY(ref byte[] data, ref int offset)
        {
            ulong Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToUInt64(data, offset);
            offset += 8;

            return Value;
        }

        private ushort ReadFieldUShort_BINARY(ref byte[] data, ref int offset)
        {
            ushort Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToUInt16(data, offset);
            offset += 2;

            return Value;
        }

        private string ReadFieldString_BINARY(ref byte[] data, ref int offset)
        {
            string Value;

            string StringValue;
            ReadStringField(ref data, ref offset, out StringValue);
            Value = StringValue;

            return Value;
        }

        private Guid ReadFieldGuid_BINARY(ref byte[] data, ref int offset)
        {
            Guid Value;

            ReadField(ref data, ref offset, 16);
            byte[] GuidBytes = new byte[16];
            for (int i = 0; i < 16; i++)
                GuidBytes[i] = data[offset++];
            Value = new Guid(GuidBytes);
            return Value;
        }

        private string ReadFieldType_BINARY(ref byte[] data, ref int offset)
        {
            string Value;

            ReadStringField(ref data, ref offset, out string AsString);
            Value = AsString;

            return Value;
        }

        private List<string> ReadFieldMembers_BINARY(ref byte[] data, ref int offset)
        {
            List<string> MemberNames = new List<string>();

            ReadField(ref data, ref offset, 4);
            int MemberCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < MemberCount; i++)
            {
                string MemberName;
                ReadStringField(ref data, ref offset, out MemberName);
                MemberNames.Add(MemberName);
            }

            return MemberNames;
        }

        private ObjectTag ReadFieldTag_BINARY(ref byte[] data, ref int offset)
        {
            ObjectTag Value;

            ReadField(ref data, ref offset, 1);
            Value = (ObjectTag)data[offset++];

            return Value;
        }

        private int ReadFieldObjectIndex_BINARY(ref byte[] data, ref int offset)
        {
            int Value;

            Value = (int)ReadFieldLong_BINARY(ref data, ref offset);

            return Value;
        }

        private long ReadFieldCount_BINARY(ref byte[] data, ref int offset)
        {
            long Value;

            Value = ReadFieldLong_BINARY(ref data, ref offset);

            return Value;
        }
#endregion

        #region Text
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
        #endregion

        private List<IDeserializedObject> DeserializedObjectList = new List<IDeserializedObject>();
    }
}
