[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test-PolySerializer")]

namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Contracts;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        #region Constants
        // Number of bytes used to store a count. Allows for collections of any size.
        private const int CountByteSize = 8;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets how objects are serialized and deserialized.
        /// </summary>
        public SerializationMode Mode { get; set; } = SerializationMode.Default;

        /// <summary>
        /// Gets or sets how objects are serialized and deserialized.
        /// </summary>
        public SerializationFormat Format { get; set; } = SerializationFormat.BinaryPreferred;

        /// <summary>
        /// Gets the output stream on which serialized data has been written to in <see cref="Serialize"/>.
        /// </summary>
        public Stream? Output { get; private set; }

        /// <summary>
        /// Gets the object serialized (after a call to <see cref="Serialize"/>) or created (after a call to <see cref="Deserialize"/>).
        /// </summary>
        public object? Root { get; private set; }

        /// <summary>
        /// Gets the input stream from which deserialized data has been read from in <see cref="Deserialize"/>.
        /// </summary>
        public Stream? Input { get; private set; }

        /// <summary>
        /// Gets or sets the Type of the <see cref="Root"/> object after a call to <see cref="Serialize"/>, or type of the object to create in <see cref="Deserialize"/>.
        /// If null, <see cref="Deserialize"/> finds the type to use from the serialized data. If not null, the serialized data must be compatible with this type or <see cref="Deserialize"/> will throw an exception.
        /// </summary>
        public Type? RootType { get; set; }

        /// <summary>
        /// Gets or sets a list of assemblies that can override the original assembly of a type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; } = new Dictionary<Assembly, Assembly>();

        /// <summary>
        /// Gets or sets a list of namespaces that can override the original namespace of a type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable { get; set; } = new Dictionary<NamespaceDescriptor, NamespaceDescriptor>();

        /// <summary>
        /// Gets or sets a list of types that can override the original type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; } = new Dictionary<Type, Type>();

        /// <summary>
        /// Gets or sets a value indicating whether arguments of generic types should be overriden.
        /// </summary>
        public bool OverrideGenericArguments { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of inserter objects that allow filling collection of items implemented using a custom type, or a type not natively supported (<seealso cref="BuiltInInserters"/>.
        /// </summary>
        public IReadOnlyList<IInserter> CustomInserters { get; set; } = new List<IInserter>();

        /// <summary>
        /// Gets a list of inserters that can add items to various types of collections.
        /// </summary>
        public IReadOnlyList<IInserter> BuiltInInserters { get; } = new List<IInserter>()
        {
            new ArrayInserter(),
            new ListInserter(),
            new GenericAddInserter(),
        };

        /// <summary>
        /// Gets the serialization or deserialization progress as a number between 0 and 1.
        /// </summary>
        public double Progress { get; private set; }

        /// <summary>
        /// Gets or sets the size of the first allocated block of data. Change this value to optimize memory or speed.
        /// </summary>
        public uint MinAllocatedSize { get; set; } = 0x10000;

        /// <summary>
        /// Gets the last value used to allocate or reallocate data. Use this information to optimize memory management.
        /// </summary>
        public uint LastAllocatedSize { get; private set; }
        #endregion

        #region Tools
        /// <summary>
        /// Finds the first serializable ancestor of <paramref name="referenceType"/>.
        /// </summary>
        /// <param name="referenceType">The type to search for serializable ancestors.</param>
        /// <returns>The first ancestor type that can be serialized, null if none. If null is returned, <paramref name="referenceType"/> cannot be serialized.</returns>
        public static Type? SerializableAncestor(Type referenceType)
        {
            Type? t = referenceType;

            while (t is not null && !t.Attributes.HasFlag(TypeAttributes.Serializable) && t.GetCustomAttribute(typeof(PolySerializer.SerializableAttribute)) is null)
                t = t.BaseType;

            return t;
        }

        /// <summary>
        /// Checks if <paramref name="referenceType"/>, a base type of <paramref name="reference"/>, is a collection type and if so returns an enumerator for <paramref name="reference"/>.
        /// </summary>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="reference">The object for which to return an enumerator if successful.</param>
        /// <param name="enumerator">The enumerator returned if successful.</param>
        /// <returns>True if <paramref name="referenceType"/> is a readable collection.</returns>
        public static bool IsReadableCollection(Type referenceType, object reference, out IEnumerator enumerator)
        {
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            Type? CurrentType;

            if (ReferenceType.IsGenericType)
                CurrentType = ReferenceType.GetGenericTypeDefinition();
            else
                CurrentType = ReferenceType;

            while (CurrentType is not null)
            {
                Type[] Interfaces = CurrentType.GetInterfaces();
                foreach (Type Interface in Interfaces)
                    if (Interface == typeof(IEnumerable))
                    {
                        enumerator = (IEnumerator)Interface.InvokeMember("GetEnumerator", BindingFlags.InvokeMethod, null, reference, new object[0], CultureInfo.InvariantCulture)!;
                        return true;
                    }

                CurrentType = CurrentType.BaseType;
            }

            Contract.Unused(out enumerator);
            return false;
        }

        /// <summary>
        /// Checks if <paramref name="referenceType"/>, a base type of <paramref name="reference"/>, is a supported collection type and if so returns an inserter for <paramref name="reference"/>.
        /// </summary>
        /// <param name="reference">The object for which to return an enumerator if successful.</param>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="inserter">The inserter returned if successful.</param>
        /// <param name="itemType">The type of items the inserter can take.</param>
        /// <returns>True if <paramref name="referenceType"/> is a writeable collection.</returns>
        public bool IsWriteableCollection(object reference, Type referenceType, out IInserter inserter, out Type itemType)
        {
            foreach (IInserter TestInserter in CustomInserters)
                if (TestInserter.TrySetReference(reference, referenceType, out Type TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }

            foreach (IInserter TestInserter in BuiltInInserters)
                if (TestInserter.TrySetReference(reference, referenceType, out Type TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }

            Contract.Unused(out inserter);
            Contract.Unused(out itemType);
            return false;
        }

        /// <summary>
        /// Checks if <paramref name="referenceType"/> is a supported collection type and if so returns the corresponding inserter.
        /// </summary>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="inserter">The inserter returned if successful.</param>
        /// <param name="itemType">The type of items the inserter can take.</param>
        /// <returns>True if <paramref name="referenceType"/> is a writeable collection.</returns>
        public bool IsWriteableCollection(Type referenceType, out IInserter inserter, out Type itemType)
        {
            foreach (IInserter TestInserter in CustomInserters)
                if (TestInserter.TryMatchType(referenceType, out Type TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }

            foreach (IInserter TestInserter in BuiltInInserters)
                if (TestInserter.TryMatchType(referenceType, out Type TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }

            Contract.Unused(out inserter);
            Contract.Unused(out itemType);
            return false;
        }
        #endregion

        #region Misc
        private int SortByName(SerializedMember p1, SerializedMember p2)
        {
            return string.Compare(p1.MemberInfo.Name, p2.MemberInfo.Name, StringComparison.InvariantCulture);
        }

        private int SortByName(DeserializedMember p1, DeserializedMember p2)
        {
            return string.Compare(p1.MemberInfo.Name, p2.MemberInfo.Name, StringComparison.InvariantCulture);
        }

        private static bool IsSerializableConstructor(ConstructorInfo constructor, Type serializedType, out List<SerializedMember> constructorParameters)
        {
            SerializableAttribute? CustomAttribute = constructor.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomAttribute is null)
            {
                Contract.Unused(out constructorParameters);
                return false;
            }

            if (CustomAttribute.Constructor.Length == 0)
            {
                Contract.Unused(out constructorParameters);
                return false;
            }

            string[] Properties = CustomAttribute.Constructor.Split(',');
            ParameterInfo[] Parameters = constructor.GetParameters();
            if (Properties.Length == 0 || Properties.Length != Parameters.Length)
            {
                Contract.Unused(out constructorParameters);
                return false;
            }

            constructorParameters = new List<SerializedMember>();
            for (int i = 0; i < Properties.Length; i++)
            {
                string PropertyName = Properties[i].Trim();
                MemberInfo[] Members = serializedType.GetMember(PropertyName);
                if (Members.Length != 1)
                    return false;

                MemberInfo Member = Members[0];
                if (Member.MemberType != MemberTypes.Property)
                    return false;

                SerializedMember NewMember = new SerializedMember(Member);
                constructorParameters.Add(NewMember);
            }

            return true;
        }

        private static bool IsSerializableMember(object reference, Type serializedType, SerializedMember newMember)
        {
            if (newMember.MemberInfo.MemberType != MemberTypes.Field && newMember.MemberInfo.MemberType != MemberTypes.Property)
                return false;

            if (IsStaticOrReadOnly(newMember.MemberInfo))
                return false;

            if (IsExcludedFromSerialization(newMember))
                return false;

            if (IsReadOnlyPropertyWithNoSetter(newMember))
                return false;

            if (IsExcludedIndexer(newMember))
                return false;

            CheckSerializationCondition(reference, serializedType, newMember);

            return true;
        }

        private static bool IsStaticOrReadOnly(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo AsFieldInfo)
            {
                if (AsFieldInfo.Attributes.HasFlag(FieldAttributes.Static) || AsFieldInfo.Attributes.HasFlag(FieldAttributes.InitOnly))
                    return true;
            }

            return false;
        }

        private static bool IsExcludedFromSerialization(SerializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
            {
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private static bool IsReadOnlyPropertyWithNoSetter(SerializedMember newMember)
        {
            if (newMember.MemberInfo is PropertyInfo AsPropertyInfo)
            {
                if (AsPropertyInfo.CanWrite)
                {
                    Contract.RequireNotNull(AsPropertyInfo.SetMethod, out MethodInfo Setter);
                    if (Setter.Attributes.HasFlag(MethodAttributes.Public))
                        return false;
                }
            }
            else
                return false;

            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
            {
                if (CustomSerializable.Setter.Length > 0)
                    return false;
            }

            return true;
        }

        private static bool IsExcludedIndexer(SerializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
                return false;

            if (newMember.MemberInfo.Name == "Item" && newMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private static void CheckSerializationCondition(object reference, Type serializedType, SerializedMember newMember)
        {
            if (newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) is SerializableAttribute CustomSerializable)
            {
                if (CustomSerializable.Condition.Length > 0)
                {
                    MemberInfo[] ConditionMembers = serializedType.GetMember(CustomSerializable.Condition);
                    if (ConditionMembers is not null)
                    {
                        foreach (MemberInfo ConditionMember in ConditionMembers)
                        {
                            if (ConditionMember is FieldInfo AsFieldInfo)
                            {
                                if (AsFieldInfo.FieldType == typeof(bool))
                                    newMember.SetCondition((bool)AsFieldInfo.GetValue(reference)!);
                            }
                            else if (ConditionMember is PropertyInfo AsPropertyInfo)
                            {
                                if (AsPropertyInfo.PropertyType == typeof(bool))
                                    newMember.SetCondition((bool)AsPropertyInfo.GetValue(reference)!);
                            }
                        }
                    }
                }
            }
        }

        private static bool ListConstructorParameters(Type serializedType, out List<SerializedMember> constructorParameters)
        {
            List<ConstructorInfo> Constructors = new List<ConstructorInfo>(serializedType.GetConstructors());

            foreach (ConstructorInfo Constructor in Constructors)
                if (IsSerializableConstructor(Constructor, serializedType, out constructorParameters))
                    return true;

            Contract.Unused(out constructorParameters);
            return false;
        }

        private static void HandleCR(byte[] data, ref int offset)
        {
            if (data[offset - 1] == '\r')
                offset++;
        }
        #endregion

        #region String conversions
        private static byte[] String2Bytes(string s)
        {
            int CharCount = s.Length;
            char[] StringChars = s.ToCharArray();

            byte[] LengthBytes = BitConverter.GetBytes(CharCount);
            byte[] StringBytes = new byte[CountByteSize + (StringChars.Length * 2)];

            for (int i = 0; i < LengthBytes.Length && i < CountByteSize; i++)
                StringBytes[i] = LengthBytes[i];

            for (int i = 0; i < StringChars.Length; i++)
            {
                byte[] Content = BitConverter.GetBytes(StringChars[i]);
                StringBytes[CountByteSize + (i * 2) + 0] = Content[0];
                StringBytes[CountByteSize + (i * 2) + 1] = Content[1];
            }

            return StringBytes;
        }

        private static string Bytes2String(int count, byte[] data, int offset)
        {
            int i = offset;
            char[] StringChars = new char[count];

            for (i = 0; i < count; i++)
                StringChars[i] = BitConverter.ToChar(data, offset + (i * 2));

            return new string(StringChars);
        }

        private static int FromHexDigit(byte[] data, int offset)
        {
            int Digit = data[offset];
            if (Digit >= '0' && Digit <= '9')
                return Digit - '0';
            else if (Digit >= 'a' && Digit <= 'f')
                return Digit - 'a' + 10;
            else if (Digit >= 'A' && Digit <= 'F')
                return Digit - 'A' + 10;
            else
                return 0;
        }

        private static int FromDecimalDigit(byte[] data, int offset)
        {
            int Digit = data[offset];
            if (Digit >= '0' && Digit <= '9')
                return Digit - '0';
            else
                return 0;
        }
        #endregion
    }
}
