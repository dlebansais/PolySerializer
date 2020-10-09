namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
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
            {
                // Takes into account the UTF-8 indicator.
                if (Data[0] == 0xEF && Data[1] == 0xBB && Data[2] == 0xBF)
                    Offset += 3;

                IsDeserializedAsText = Data[Offset] == 'M' && Data[Offset + 1] == 'o' && Data[Offset + 2] == 'd' && Data[Offset + 3] == 'e';
            }
            else
                IsDeserializedAsText = Format == SerializationFormat.TextOnly;

            if (IsDeserializedAsText)
                return INTERNAL_Deserialize_TEXT(ref Data, ref Offset);
            else
                return INTERNAL_Deserialize_BINARY(ref Data, ref Offset);
        }

        private void ReadStringField(ref byte[] data, ref int offset, out string? value)
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

        private static void CreateObject(Type referenceType, out object reference)
        {
            reference = Activator.CreateInstance(referenceType) !;
        }

        private static void CreateObject(Type referenceType, object?[] parameters, out object reference)
        {
            reference = Activator.CreateInstance(referenceType, parameters) !;
        }

        private static void CreateObject(Type valueType, long count, out object reference)
        {
            if (valueType.IsArray)
            {
                Type ArrayType = valueType.GetElementType() !;
                reference = Array.CreateInstance(ArrayType, count) !;
            }
            else if (count < int.MaxValue)
            {
                reference = Activator.CreateInstance(valueType, (int)count) !;
            }
            else
                reference = Activator.CreateInstance(valueType, count) !;
        }

        private static Type DeserializedTrueType(string typeName)
        {
            return Type.GetType(typeName) !;
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
                        Type? UpdatedType = Assembly.GetType(Type.FullName!);
                        if (UpdatedType != null)
                            TypeList[i] = UpdatedType;
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

        private static void DeconstructType(Type type, out Type[] typeList)
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

        private static void ReconstructType(Type[] typeList, out Type type)
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

        private static bool IsDeserializableMember(Type deserializedType, DeserializedMember newMember)
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

        private static bool IsExcludedFromDeserialization(DeserializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
            {
                if (CustomSerializable.ExcludeX)
                    return true;
            }

            return false;
        }

        private static bool IsReadOnlyPropertyWithNoValidSetter(Type deserializedType, DeserializedMember newMember)
        {
            if (newMember.MemberInfo is PropertyInfo AsPropertyInfo)
            {
                if (AsPropertyInfo.CanWrite)
                {
                    Debug.Assert(AsPropertyInfo.SetMethod != null);
                    MethodInfo Setter = AsPropertyInfo.SetMethod;
                    if (Setter.Attributes.HasFlag(MethodAttributes.Public))
                        return false;
                }

                if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
                {
                    if (CustomSerializable.SetterX.Length > 0)
                    {
                        MemberInfo[] SetterMembers = deserializedType.GetMember(CustomSerializable.SetterX);
                        if (SetterMembers != null)
                        {
                            Type ExpectedParameterType = AsPropertyInfo.PropertyType;

                            foreach (MemberInfo SetterMember in SetterMembers)
                            {
                                if (SetterMember is MethodInfo AsMethodInfo)
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

        private static bool IsExcludedIndexer(DeserializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
                return false;

            if (newMember.MemberInfo.Name == "Item" && newMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private static void CheckForSerializedCondition(DeserializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
            {
                if (CustomSerializable.ConditionX.Length > 0)
                    newMember.SetHasCondition();
            }
        }

        private void ReadField(ref byte[] data, ref int offset, int minLength)
        {
            bool Reload = false;
            Stream InputStream = Input !;

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
                long Length = InputStream.Length - InputStream.Position;
                if (Length > data.Length - offset)
                    Length = data.Length - offset;

                InputStream.Read(data, offset, (int)Length);
                offset = 0;
            }
        }

        private List<IDeserializedObject> DeserializedObjectList = new List<IDeserializedObject>();
    }
}
