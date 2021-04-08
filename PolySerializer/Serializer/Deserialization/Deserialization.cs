namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Contracts;

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
                HandleUTF8Indicator(Data, ref Offset);

                IsDeserializedAsText = Data[Offset] == 'M' && Data[Offset + 1] == 'o' && Data[Offset + 2] == 'd' && Data[Offset + 3] == 'e';
            }
            else
            {
                IsDeserializedAsText = Format == SerializationFormat.TextOnly;

                if (IsDeserializedAsText)
                    HandleUTF8Indicator(Data, ref Offset);
            }

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
            reference = Activator.CreateInstance(referenceType)!;
        }

        private static void CreateObject(Type referenceType, object?[] parameters, out object reference)
        {
            reference = Activator.CreateInstance(referenceType, parameters)!;
        }

        private static void CreateObject(Type valueType, long count, out object reference)
        {
            if (valueType.IsArray)
            {
                Type ArrayType = valueType.GetElementType()!;
                reference = Array.CreateInstance(ArrayType, count)!;
            }
            else
            {
                bool HasParameterlessConstructor = false;
                bool HasConstructorIntCount = false;
                bool HasConstructorLongCount = false;

                ConstructorInfo[] Constructors = valueType.GetConstructors();
                foreach (ConstructorInfo Constructor in Constructors)
                {
                    ParameterInfo[] Parameters = Constructor.GetParameters();

                    if (Parameters.Length == 0)
                        HasParameterlessConstructor = true;
                    else if (Parameters.Length == 1)
                    {
                        Type ParameterType = Parameters[0].ParameterType;
                        HasConstructorIntCount |= ParameterType == typeof(int);
                        HasConstructorLongCount |= ParameterType == typeof(long);
                    }
                }

                if (HasConstructorIntCount && count >= int.MinValue && count <= int.MaxValue)
                    reference = Activator.CreateInstance(valueType, (int)count)!;
                else if (HasConstructorLongCount)
                    reference = Activator.CreateInstance(valueType, count)!;
                else
                {
                    Debug.Assert(HasParameterlessConstructor);
                    reference = Activator.CreateInstance(valueType)!;
                }
            }
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
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private static bool IsReadOnlyPropertyWithNoValidSetter(Type deserializedType, DeserializedMember newMember)
        {
            if (newMember.MemberInfo is PropertyInfo AsPropertyInfo)
                return IsReadOnlyPropertyWithNoValidSetter(deserializedType, newMember, AsPropertyInfo);
            else
                return false;
        }

        private static bool IsReadOnlyPropertyWithNoValidSetter(Type deserializedType, DeserializedMember newMember, PropertyInfo propertyInfo)
        {
            if (propertyInfo.CanWrite)
            {
                Contract.RequireNotNull(propertyInfo.SetMethod, out MethodInfo Setter);
                if (Setter.Attributes.HasFlag(MethodAttributes.Public))
                    return false;
            }

            if (propertyInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable && CustomSerializable.Setter.Length > 0)
                return IsReadOnlyPropertyWithCustomSerializable(deserializedType, newMember, propertyInfo, CustomSerializable);

            return true;
        }

        private static bool IsReadOnlyPropertyWithCustomSerializable(Type deserializedType, DeserializedMember newMember, PropertyInfo propertyInfo, SerializableAttribute customSerializable)
        {
            bool Result = true;

            MemberInfo[] SetterMembers = deserializedType.GetMember(customSerializable.Setter);
            if (SetterMembers != null)
            {
                Type ExpectedParameterType = propertyInfo.PropertyType;

                foreach (MemberInfo SetterMember in SetterMembers)
                    if (SetterMember is MethodInfo AsMethodInfo)
                    {
                        ParameterInfo[] Parameters = AsMethodInfo.GetParameters();
                        if (Parameters != null && Parameters.Length == 1)
                        {
                            ParameterInfo Parameter = Parameters[0];
                            if (Parameter.ParameterType == ExpectedParameterType)
                            {
                                newMember.SetPropertySetter(AsMethodInfo);
                                Result = false;
                                break;
                            }
                        }
                    }
            }

            return Result;
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
                if (CustomSerializable.Condition.Length > 0)
                    newMember.SetHasCondition();
            }
        }

        private void ReadField(ref byte[] data, ref int offset, int minLength)
        {
            bool Reload = false;
            Stream InputStream = Input !;

            if (offset + minLength > data.Length)
            {
                byte[] NewData = data;

                if (data.Length < minLength)
                    NewData = new byte[minLength];

                int i;
                for (i = 0; i < data.Length - offset; i++)
                    NewData[i] = data[i + offset];

                data = NewData;
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
