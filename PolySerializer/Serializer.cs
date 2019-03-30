namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    #region Interface
    /// <summary>
    ///     Public interface of the serializer.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        ///     Defines how objects are serialized and deserialized.
        /// </summary>
        SerializationMode Mode { get; set; }

        /// <summary>
        ///     Defines how objects are serialized and deserialized.
        /// </summary>
        SerializationFormat Format { get; set; }

        /// <summary>
        ///     Serializes <paramref name="root"/> and write the serialized data to <paramref name="output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// </parameters>
        void Serialize(Stream output, object root);

        /// <summary>
        ///     Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        /// <returns>
        ///     The deserialized object.
        /// </returns>
        object Deserialize(Stream input);

        /// <summary>
        ///     Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        /// <returns>
        ///     True of the stream can be deserialized, False otherwise.
        /// </returns>
        bool Check(Stream input);

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
        Task SerializeAsync(Stream output, object root);

        /// <summary>
        ///     Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        Task<object> DeserializeAsync(Stream input);

        /// <summary>
        ///     Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        Task<bool> CheckAsync(Stream input);

        /// <summary>
        ///     The output stream on which serialized data has been written to in <see cref="Serialize"/>.
        /// </summary>
        Stream Output { get; }

        /// <summary>
        ///     The object serialized (after a call to <see cref="Serialize"/>) or created (after a call to <see cref="Deserialize"/>).
        /// </summary>
        object Root { get; }

        /// <summary>
        ///     The input stream from which deserialized data has been read from in <see cref="Deserialize"/>.
        /// </summary>
        Stream Input { get; }

        /// <summary>
        ///     Type of the <see cref="Root"/> object after a call to <see cref="Serialize"/>, or type of the object to create in <see cref="Deserialize"/>.
        ///     If null, <see cref="Deserialize"/> finds the type to use from the serialized data. If not null, the serialized data must be compatible with this type or <see cref="Deserialize"/> will throw an exception.
        /// </summary>
        Type RootType { get; set; }

        /// <summary>
        ///     The serialization or deserialization progress as a number between 0 and 1.
        /// </summary>
        double Progress { get; }

        /// <summary>
        /// Sets or gets a list of assemblies that can override the original assembly of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; }

        /// <summary>
        /// Sets or gets a list of namespaces that can override the original namespace of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable { get; set; }

        /// <summary>
        /// Gets or sets a list of types that can override the original type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; }

        /// <summary>
        /// Gets or sets a flag to indicate if argument of generic types should be overriden.
        /// </summary>
        bool OverrideGenericArguments { get; set; }

        /// <summary>
        /// Gets or sets a list of inserter objects that allow filling collection of items implemented using a custom type, or a type not natively supported (<seealso cref="BuiltInInserters"/>.
        /// </summary>
        IReadOnlyList<IInserter> CustomInserters { get; set; }

        /// <summary>
        /// Gets a list of inserters that can add items to various types of collections.
        /// </summary>
        IReadOnlyList<IInserter> BuiltInInserters { get; }
    }
    #endregion

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public class Serializer : ISerializer
    {
        #region Constants
        // Number of bytes used to store a count. Allows for collections of any size.
        private const int CountByteSize = 8;
        #endregion

        #region Properties
        /// <summary>
        ///     Defines how objects are serialized and deserialized.
        /// </summary>
        public SerializationMode Mode { get; set; } = SerializationMode.Default;

        /// <summary>
        ///     Defines how objects are serialized and deserialized.
        /// </summary>
        public SerializationFormat Format { get; set; } = SerializationFormat.BinaryPreferred;

        /// <summary>
        ///     The output stream on which serialized data has been written to in <see cref="Serialize"/>.
        /// </summary>
        public Stream Output { get; private set; } = null;

        /// <summary>
        ///     The object serialized (after a call to <see cref="Serialize"/>) or created (after a call to <see cref="Deserialize"/>).
        /// </summary>
        public object Root { get; private set; } = null;

        /// <summary>
        ///     The input stream from which deserialized data has been read from in <see cref="Deserialize"/>.
        /// </summary>
        public Stream Input { get; private set; } = null;

        /// <summary>
        ///     Type of the <see cref="Root"/> object after a call to <see cref="Serialize"/>, or type of the object to create in <see cref="Deserialize"/>.
        ///     If null, <see cref="Deserialize"/> finds the type to use from the serialized data. If not null, the serialized data must be compatible with this type or <see cref="Deserialize"/> will throw an exception.
        /// </summary>
        public Type RootType { get; set; } = null;

        /// <summary>
        /// Sets or gets a list of assemblies that can override the original assembly of a type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; } = new Dictionary<Assembly, Assembly>();

        /// <summary>
        /// Sets or gets a list of namespaces that can override the original namespace of a type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable { get; set; } = new Dictionary<NamespaceDescriptor, NamespaceDescriptor>();

        /// <summary>
        /// Gets or sets a list of types that can override the original type during deserialization.
        /// </summary>
        public IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; } = new Dictionary<Type, Type>();

        /// <summary>
        /// Gets or sets a flag to indicate if argument of generic types should be overriden.
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
        ///     The serialization or deserialization progress as a number between 0 and 1.
        /// </summary>
        public double Progress { get; private set; }

        /// <summary>
        ///     Size of the first allocated block of data. Change this value to optimize memory or speed.
        /// </summary>
        public uint MinAllocatedSize { get; set; } = 0x10000;

        /// <summary>
        ///     Last value used to allocate or reallocate data. Use this information to optimize memory management.
        /// </summary>
        public uint LastAllocatedSize { get; private set; } = 0;
        #endregion

        #region Serialization
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
            Type ValueType = value.GetType();

            if (ValueType == typeof(sbyte))
                AddFieldSByte_BINARY(ref data, ref offset, (sbyte)value);
            else if (ValueType == typeof(byte))
                AddFieldByte_BINARY(ref data, ref offset, (byte)value);
            else if (ValueType == typeof(bool))
                AddFieldBool_BINARY(ref data, ref offset, (bool)value);
            else if (ValueType == typeof(char))
                AddFieldChar_BINARY(ref data, ref offset, (char)value);
            else if (ValueType == typeof(decimal))
                AddFieldDecimal_BINARY(ref data, ref offset, (decimal)value);
            else if (ValueType == typeof(double))
                AddFieldDouble_BINARY(ref data, ref offset, (double)value);
            else if (ValueType == typeof(float))
                AddFieldFloat_BINARY(ref data, ref offset, (float)value);
            else if (ValueType == typeof(int))
                AddFieldInt_BINARY(ref data, ref offset, (int)value);
            else if (ValueType == typeof(long))
                AddFieldLong_BINARY(ref data, ref offset, (long)value);
            else if (ValueType == typeof(short))
                AddFieldShort_BINARY(ref data, ref offset, (short)value);
            else if (ValueType == typeof(uint))
                AddFieldUInt_BINARY(ref data, ref offset, (uint)value);
            else if (ValueType == typeof(ulong))
                AddFieldULong_BINARY(ref data, ref offset, (ulong)value);
            else if (ValueType == typeof(ushort))
                AddFieldUShort_BINARY(ref data, ref offset, (ushort)value);
            else if (ValueType == typeof(string))
                AddFieldString_BINARY(ref data, ref offset, (string)value);
            else if (ValueType == typeof(Guid))
                AddFieldGuid_BINARY(ref data, ref offset, (Guid)value);
            else if (ValueType.IsEnum)
                SerializeEnumType_BINARY(value, ValueType, ref data, ref offset);
            else
                return false;

            return true;
        }

        private void SerializeEnumType_BINARY(object value, Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            if (UnderlyingSystemType == typeof(sbyte))
                AddFieldSByte_BINARY(ref data, ref offset, (sbyte)value);
            else if (UnderlyingSystemType == typeof(byte))
                AddFieldByte_BINARY(ref data, ref offset, (byte)value);
            else if (UnderlyingSystemType == typeof(short))
                AddFieldShort_BINARY(ref data, ref offset, (short)value);
            else if (UnderlyingSystemType == typeof(ushort))
                AddFieldUShort_BINARY(ref data, ref offset, (ushort)value);
            else if (UnderlyingSystemType == typeof(int))
                AddFieldInt_BINARY(ref data, ref offset, (int)value);
            else if (UnderlyingSystemType == typeof(uint))
                AddFieldUInt_BINARY(ref data, ref offset, (uint)value);
            else if (UnderlyingSystemType == typeof(long))
                AddFieldLong_BINARY(ref data, ref offset, (long)value);
            else if (UnderlyingSystemType == typeof(ulong))
                AddFieldULong_BINARY(ref data, ref offset, (ulong)value);
            else
                AddFieldInt_BINARY(ref data, ref offset, (int)value);
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
            Type ValueType = value.GetType();

            if (ValueType == typeof(sbyte))
                AddFieldSByte_TEXT(ref data, ref offset, (sbyte)value);
            else if (ValueType == typeof(byte))
                AddFieldByte_TEXT(ref data, ref offset, (byte)value);
            else if (ValueType == typeof(bool))
                AddFieldBool_TEXT(ref data, ref offset, (bool)value);
            else if (ValueType == typeof(char))
                AddFieldChar_TEXT(ref data, ref offset, (char)value);
            else if (ValueType == typeof(decimal))
                AddFieldDecimal_TEXT(ref data, ref offset, (decimal)value);
            else if (ValueType == typeof(double))
                AddFieldDouble_TEXT(ref data, ref offset, (double)value);
            else if (ValueType == typeof(float))
                AddFieldFloat_TEXT(ref data, ref offset, (float)value);
            else if (ValueType == typeof(int))
                AddFieldInt_TEXT(ref data, ref offset, (int)value);
            else if (ValueType == typeof(long))
                AddFieldLong_TEXT(ref data, ref offset, (long)value);
            else if (ValueType == typeof(short))
                AddFieldShort_TEXT(ref data, ref offset, (short)value);
            else if (ValueType == typeof(uint))
                AddFieldUInt_TEXT(ref data, ref offset, (uint)value);
            else if (ValueType == typeof(ulong))
                AddFieldULong_TEXT(ref data, ref offset, (ulong)value);
            else if (ValueType == typeof(ushort))
                AddFieldUShort_TEXT(ref data, ref offset, (ushort)value);
            else if (ValueType == typeof(string))
                AddFieldString_TEXT(ref data, ref offset, (string)value);
            else if (ValueType == typeof(Guid))
                AddFieldGuid_TEXT(ref data, ref offset, (Guid)value);
            else if (ValueType.IsEnum)
                SerializeEnumType_TEXT(value, ValueType, ref data, ref offset);
            else
                return false;

            return true;
        }

        private void SerializeEnumType_TEXT(object value, Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();

            if (UnderlyingSystemType == typeof(sbyte))
                AddFieldSByte_TEXT(ref data, ref offset, (sbyte)value);
            else if (UnderlyingSystemType == typeof(byte))
                AddFieldByte_TEXT(ref data, ref offset, (byte)value);
            else if (UnderlyingSystemType == typeof(short))
                AddFieldShort_TEXT(ref data, ref offset, (short)value);
            else if (UnderlyingSystemType == typeof(ushort))
                AddFieldUShort_TEXT(ref data, ref offset, (ushort)value);
            else if (UnderlyingSystemType == typeof(int))
                AddFieldInt_TEXT(ref data, ref offset, (int)value);
            else if (UnderlyingSystemType == typeof(uint))
                AddFieldUInt_TEXT(ref data, ref offset, (uint)value);
            else if (UnderlyingSystemType == typeof(long))
                AddFieldLong_TEXT(ref data, ref offset, (long)value);
            else if (UnderlyingSystemType == typeof(ulong))
                AddFieldULong_TEXT(ref data, ref offset, (ulong)value);
            else
                AddFieldInt_TEXT(ref data, ref offset, (int)value);
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
        #endregion

        #region Deserialization
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
            if (valueType == typeof(sbyte))
                value = ReadFieldSByte_BINARY(ref data, ref offset);
            else if (valueType == typeof(byte))
                value = ReadFieldByte_BINARY(ref data, ref offset);
            else if (valueType == typeof(bool))
                value = ReadFieldBool_BINARY(ref data, ref offset);
            else if (valueType == typeof(char))
                value = ReadFieldChar_BINARY(ref data, ref offset);
            else if (valueType == typeof(decimal))
                value = ReadFieldDecimal_BINARY(ref data, ref offset);
            else if (valueType == typeof(double))
                value = ReadFieldDouble_BINARY(ref data, ref offset);
            else if (valueType == typeof(float))
                value = ReadFieldFloat_BINARY(ref data, ref offset);
            else if (valueType == typeof(int))
                value = ReadFieldInt_BINARY(ref data, ref offset);
            else if (valueType == typeof(long))
                value = ReadFieldLong_BINARY(ref data, ref offset);
            else if (valueType == typeof(short))
                value = ReadFieldShort_BINARY(ref data, ref offset);
            else if (valueType == typeof(uint))
                value = ReadFieldUInt_BINARY(ref data, ref offset);
            else if (valueType == typeof(ulong))
                value = ReadFieldULong_BINARY(ref data, ref offset);
            else if (valueType == typeof(ushort))
                value = ReadFieldUShort_BINARY(ref data, ref offset);
            else if (valueType == typeof(string))
                value = ReadFieldString_BINARY(ref data, ref offset);
            else if (valueType == typeof(Guid))
                value = ReadFieldGuid_BINARY(ref data, ref offset);
            else if (valueType != null && valueType.IsEnum)
                DeserializeEnumType_BINARY(valueType, ref data, ref offset, out value);
            else
            {
                value = null;
                return false;
            }

            return true;
        }

        private void DeserializeEnumType_BINARY(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();
            if (UnderlyingSystemType == typeof(sbyte))
                value = Enum.ToObject(valueType, ReadFieldSByte_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(byte))
                value = Enum.ToObject(valueType, ReadFieldByte_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(short))
                value = Enum.ToObject(valueType, ReadFieldShort_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ushort))
                value = Enum.ToObject(valueType, ReadFieldUShort_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(int))
                value = Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(uint))
                value = Enum.ToObject(valueType, ReadFieldUInt_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(long))
                value = Enum.ToObject(valueType, ReadFieldLong_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ulong))
                value = Enum.ToObject(valueType, ReadFieldULong_BINARY(ref data, ref offset));
            else
                value = Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
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
            if (valueType == typeof(sbyte))
                value = ReadFieldSByte_TEXT(ref data, ref offset);
            else if (valueType == typeof(byte))
                value = ReadFieldByte_TEXT(ref data, ref offset);
            else if (valueType == typeof(bool))
                value = ReadFieldBool_TEXT(ref data, ref offset);
            else if (valueType == typeof(char))
                value = ReadFieldChar_TEXT(ref data, ref offset);
            else if (valueType == typeof(decimal))
                value = ReadFieldDecimal_TEXT(ref data, ref offset);
            else if (valueType == typeof(double))
                value = ReadFieldDouble_TEXT(ref data, ref offset);
            else if (valueType == typeof(float))
                value = ReadFieldFloat_TEXT(ref data, ref offset);
            else if (valueType == typeof(int))
                value = ReadFieldInt_TEXT(ref data, ref offset);
            else if (valueType == typeof(long))
                value = ReadFieldLong_TEXT(ref data, ref offset);
            else if (valueType == typeof(short))
                value = ReadFieldShort_TEXT(ref data, ref offset);
            else if (valueType == typeof(uint))
                value = ReadFieldUInt_TEXT(ref data, ref offset);
            else if (valueType == typeof(ulong))
                value = ReadFieldULong_TEXT(ref data, ref offset);
            else if (valueType == typeof(ushort))
                value = ReadFieldUShort_TEXT(ref data, ref offset);
            else if (valueType == typeof(string))
                value = ReadFieldString_TEXT(ref data, ref offset);
            else if (valueType == typeof(Guid))
                value = ReadFieldGuid_TEXT(ref data, ref offset);
            else if (valueType != null && valueType.IsEnum)
                DeserializeEnumType_TEXT(valueType, ref data, ref offset, out value);
            else
            {
                value = null;
                return false;
            }

            return true;
        }

        private void DeserializeEnumType_TEXT(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();
            if (UnderlyingSystemType == typeof(sbyte))
                value = Enum.ToObject(valueType, ReadFieldSByte_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(byte))
                value = Enum.ToObject(valueType, ReadFieldByte_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(short))
                value = Enum.ToObject(valueType, ReadFieldShort_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ushort))
                value = Enum.ToObject(valueType, ReadFieldUShort_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(int))
                value = Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(uint))
                value = Enum.ToObject(valueType, ReadFieldUInt_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(long))
                value = Enum.ToObject(valueType, ReadFieldLong_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ulong))
                value = Enum.ToObject(valueType, ReadFieldULong_TEXT(ref data, ref offset));
            else
                value = Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
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
#endregion

#region Check
        /// <summary>
        ///     Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        /// <returns>
        ///     True of the stream can be deserialized, False otherwise.
        /// </returns>
        public bool Check(Stream input)
        {
            InitializeCheck(input);
            return INTERNAL_Check();
        }

        /// <summary>
        ///     Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        public Task<bool> CheckAsync(Stream input)
        {
            InitializeCheck(input);
            return Task.Run(() => INTERNAL_Check());
        }

        private void InitializeCheck(Stream input)
        {
            Input = input;
            Progress = 0;
        }

        private bool INTERNAL_Check()
        {
            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            ReadField(ref Data, ref Offset, 4);

            bool IsCheckedAsText;
            if (Format == SerializationFormat.TextPreferred || Format == SerializationFormat.BinaryPreferred)
                IsCheckedAsText = Data[0] == 'M' && Data[1] == 'o' && Data[2] == 'd' && Data[3] == 'e';
            else
                IsCheckedAsText = Format == SerializationFormat.TextOnly;

            if (IsCheckedAsText)
                return INTERNAL_Check_TEXT(ref Data, ref Offset);
            else
                return INTERNAL_Check_BINARY(ref Data, ref Offset);
        }

        private void AddCheckedObject(Type checkedType, long count)
        {
            CheckedObjectList.Add(new CheckedObject(checkedType, count));
        }

#region Binary
        private bool INTERNAL_Check_BINARY(ref byte[] data, ref int offset)
        {
            Mode = (SerializationMode)BitConverter.ToInt32(data, offset);
            offset += 4;

            CheckedObjectList.Clear();

            if (!ProcessCheckable_BINARY(RootType, ref data, ref offset))
                return false;

            if (RootType == null)
                return false;

            bool Success = true;
            int i = 0;
            while (i < CheckedObjectList.Count)
            {
                Progress = i / (double)CheckedObjectList.Count;

                ICheckedObject NextChecked = CheckedObjectList[i++];
                if (!Check_BINARY(NextChecked.CheckedType, NextChecked.Count, ref data, ref offset, NextChecked))
                {
                    Success = false;
                    break;
                }
            }

            Progress = 1.0;

            return Success;
        }

        private bool Check_BINARY(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject nextChecked)
        {
            if (!CheckCollection_BINARY(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType);
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_BINARY(CheckedType, ref data, ref offset);

            foreach (DeserializedMember Member in CheckedMembers)
            {
                if (Member.HasCondition)
                {
                    bool ConditionValue = ReadFieldBool_BINARY(ref data, ref offset);
                    if (!ConditionValue)
                        continue;
                }

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

                if (!ProcessCheckable_BINARY(MemberType, ref data, ref offset))
                    return false;
            }

            if (nextChecked != null)
                nextChecked.SetChecked();

            return true;
        }

        private bool CheckCollection_BINARY(Type referenceType, long count, ref byte[] data, ref int offset)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (!ProcessCheckable_BINARY(ItemType, ref data, ref offset))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool CheckBasicType_BINARY(Type valueType, ref byte[] data, ref int offset)
        {
            if (valueType == typeof(sbyte))
                ReadFieldSByte_BINARY(ref data, ref offset);
            else if (valueType == typeof(byte))
                ReadFieldByte_BINARY(ref data, ref offset);
            else if (valueType == typeof(bool))
                ReadFieldBool_BINARY(ref data, ref offset);
            else if (valueType == typeof(char))
                ReadFieldChar_BINARY(ref data, ref offset);
            else if (valueType == typeof(decimal))
                ReadFieldDecimal_BINARY(ref data, ref offset);
            else if (valueType == typeof(double))
                ReadFieldDouble_BINARY(ref data, ref offset);
            else if (valueType == typeof(float))
                ReadFieldFloat_BINARY(ref data, ref offset);
            else if (valueType == typeof(int))
                ReadFieldInt_BINARY(ref data, ref offset);
            else if (valueType == typeof(long))
                ReadFieldLong_BINARY(ref data, ref offset);
            else if (valueType == typeof(short))
                ReadFieldShort_BINARY(ref data, ref offset);
            else if (valueType == typeof(uint))
                ReadFieldUInt_BINARY(ref data, ref offset);
            else if (valueType == typeof(ulong))
                ReadFieldULong_BINARY(ref data, ref offset);
            else if (valueType == typeof(ushort))
                ReadFieldUShort_BINARY(ref data, ref offset);
            else if (valueType == typeof(string))
                ReadFieldString_BINARY(ref data, ref offset);
            else if (valueType == typeof(Guid))
                ReadFieldGuid_BINARY(ref data, ref offset);
            else if (valueType != null && valueType.IsEnum)
                CheckEnumType_BINARY(valueType, ref data, ref offset);
            else
                return false;

            return true;
        }

        private void CheckEnumType_BINARY(Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();
            if (UnderlyingSystemType == typeof(sbyte))
                Enum.ToObject(valueType, ReadFieldSByte_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(byte))
                Enum.ToObject(valueType, ReadFieldByte_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(short))
                Enum.ToObject(valueType, ReadFieldShort_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ushort))
                Enum.ToObject(valueType, ReadFieldUShort_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(int))
                Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(uint))
                Enum.ToObject(valueType, ReadFieldUInt_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(long))
                Enum.ToObject(valueType, ReadFieldLong_BINARY(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ulong))
                Enum.ToObject(valueType, ReadFieldULong_BINARY(ref data, ref offset));
            else
                Enum.ToObject(valueType, ReadFieldInt_BINARY(ref data, ref offset));
        }

        private bool ProcessCheckable_BINARY(Type referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_BINARY(referenceType, ref data, ref offset))
                return true;

            string ReferenceTypeName = ReadFieldType_BINARY(ref data, ref offset);
            if (ReferenceTypeName == null)
                return true;

            OverrideTypeName(ref ReferenceTypeName);
            referenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = referenceType;
            OverrideType(ref referenceType);
            Type NewType = referenceType;
            referenceType = OriginalType;

            if (referenceType.IsValueType)
            {
                if (!Check_BINARY(referenceType, -1, ref data, ref offset, null))
                    return false;
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_BINARY(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    ReadFieldObjectIndex_BINARY(ref data, ref offset);
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    AddCheckedObject(referenceType, -1);
                }
                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount_BINARY(ref data, ref offset);
                    AddCheckedObject(referenceType, Count);
                }
                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(referenceType, out ConstructorParameters))
                    {
                        for (int i = 0; i < ConstructorParameters.Count; i++)
                        {
                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            Type MemberType = AsPropertyInfo.PropertyType;
                            if (!ProcessCheckable_BINARY(MemberType, ref data, ref offset))
                                return false;
                        }

                        AddCheckedObject(referenceType, -1);
                    }
                }
            }

            return true;
        }
#endregion

#region Text
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

            CheckedObjectList.Clear();

            if (!ProcessCheckable_TEXT(RootType, ref data, ref offset))
                return false;

            if (RootType == null)
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

        private bool Check_TEXT(Type referenceType, long count, ref byte[] data, ref int offset, ICheckedObject nextChecked)
        {
            if (!CheckCollection_TEXT(referenceType, count, ref data, ref offset))
                return false;

            Type CheckedType = SerializableAncestor(referenceType);
            List<DeserializedMember> CheckedMembers = ListDeserializedMembers_TEXT(CheckedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in CheckedMembers)
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

                if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                    return false;
            }

            if (nextChecked != null)
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

        private bool CheckBasicType_TEXT(Type valueType, ref byte[] data, ref int offset)
        {
            if (valueType == typeof(sbyte))
                ReadFieldSByte_TEXT(ref data, ref offset);
            else if (valueType == typeof(byte))
                ReadFieldByte_TEXT(ref data, ref offset);
            else if (valueType == typeof(bool))
                ReadFieldBool_TEXT(ref data, ref offset);
            else if (valueType == typeof(char))
                ReadFieldChar_TEXT(ref data, ref offset);
            else if (valueType == typeof(decimal))
                ReadFieldDecimal_TEXT(ref data, ref offset);
            else if (valueType == typeof(double))
                ReadFieldDouble_TEXT(ref data, ref offset);
            else if (valueType == typeof(float))
                ReadFieldFloat_TEXT(ref data, ref offset);
            else if (valueType == typeof(int))
                ReadFieldInt_TEXT(ref data, ref offset);
            else if (valueType == typeof(long))
                ReadFieldLong_TEXT(ref data, ref offset);
            else if (valueType == typeof(short))
                ReadFieldShort_TEXT(ref data, ref offset);
            else if (valueType == typeof(uint))
                ReadFieldUInt_TEXT(ref data, ref offset);
            else if (valueType == typeof(ulong))
                ReadFieldULong_TEXT(ref data, ref offset);
            else if (valueType == typeof(ushort))
                ReadFieldUShort_TEXT(ref data, ref offset);
            else if (valueType == typeof(string))
                ReadFieldString_TEXT(ref data, ref offset);
            else if (valueType == typeof(Guid))
                ReadFieldGuid_TEXT(ref data, ref offset);
            else if (valueType != null && valueType.IsEnum)
                CheckEnumType_TEXT(valueType, ref data, ref offset);
            else
                return false;

            return true;
        }

        private void CheckEnumType_TEXT(Type valueType, ref byte[] data, ref int offset)
        {
            Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();
            if (UnderlyingSystemType == typeof(sbyte))
                Enum.ToObject(valueType, ReadFieldSByte_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(byte))
                Enum.ToObject(valueType, ReadFieldByte_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(short))
                Enum.ToObject(valueType, ReadFieldShort_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ushort))
                Enum.ToObject(valueType, ReadFieldUShort_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(int))
                Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(uint))
                Enum.ToObject(valueType, ReadFieldUInt_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(long))
                Enum.ToObject(valueType, ReadFieldLong_TEXT(ref data, ref offset));
            else if (UnderlyingSystemType == typeof(ulong))
                Enum.ToObject(valueType, ReadFieldULong_TEXT(ref data, ref offset));
            else
                Enum.ToObject(valueType, ReadFieldInt_TEXT(ref data, ref offset));
        }

        private bool ProcessCheckable_TEXT(Type referenceType, ref byte[] data, ref int offset)
        {
            if (CheckBasicType_TEXT(referenceType, ref data, ref offset))
                return true;

            string ReferenceTypeName = ReadFieldType_TEXT(ref data, ref offset);
            if (ReferenceTypeName == null)
                return true;

            OverrideTypeName(ref ReferenceTypeName);
            referenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = referenceType;
            OverrideType(ref referenceType);
            Type NewType = referenceType;
            referenceType = OriginalType;

            if (referenceType.IsValueType)
            {
                if (!Check_TEXT(referenceType, -1, ref data, ref offset, null))
                    return false;
            }
            else
            {
                ObjectTag ReferenceTag = ReadFieldTag_TEXT(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    ReadFieldObjectIndex_TEXT(ref data, ref offset);
                }
                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    AddCheckedObject(referenceType, -1);
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

                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            Type MemberType = AsPropertyInfo.PropertyType;
                            if (!ProcessCheckable_TEXT(MemberType, ref data, ref offset))
                                return false;
                        }

                        ReadSeparator_TEXT(ref data, ref offset);

                        AddCheckedObject(referenceType, -1);
                    }
                }
            }

            return true;
        }
#endregion

        private List<ICheckedObject> CheckedObjectList = new List<ICheckedObject>();
#endregion

#region Tools
        /// <summary>
        ///     Finds the first serializable ancestor of <paramref name="referenceType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="referenceType">The type to search for serializable ancestors.</param>
        /// </parameters>
        /// <returns>
        ///     The first ancestor type that can be serialized, null if none. If null is returned, <paramref name="referenceType"/> cannot be serialized.
        /// </returns>
        public static Type SerializableAncestor(Type referenceType)
        {
            Type t = referenceType;

            while (t != null && !t.Attributes.HasFlag(TypeAttributes.Serializable) && t.GetCustomAttribute(typeof(PolySerializer.SerializableAttribute)) == null)
                t = t.BaseType;

            return t;
        }

        /// <summary>
        ///     Checks if <paramref name="referenceType"/>, a base type of <paramref name="reference"/>, is a collection type and if so returns an enumerator for <paramref name="reference"/>.
        /// </summary>
        /// <parameters>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="reference">The object for which to return an enumerator if successful.</param>
        /// <param name="enumerator">The enumerator returned if successful.</param>
        /// </parameters>
        /// <returns>
        ///     True if <paramref name="referenceType"/> is a readable collection.
        /// </returns>
        public static bool IsReadableCollection(Type referenceType, object reference, out IEnumerator enumerator)
        {
            Type CurrentType;

            if (referenceType.IsGenericType)
                CurrentType = referenceType.GetGenericTypeDefinition();
            else
                CurrentType = referenceType;

            while (CurrentType != null)
            {
                Type[] Interfaces = CurrentType.GetInterfaces();
                foreach (Type Interface in Interfaces)
                    if (Interface == typeof(IEnumerable))
                    {
                        enumerator = Interface.InvokeMember("GetEnumerator", BindingFlags.Public, null, reference, null) as IEnumerator;
                        return true;
                    }

                CurrentType = CurrentType.BaseType;
            }

            enumerator = null;
            return false;
        }

        /// <summary>
        ///     Checks if <paramref name="referenceType"/>, a base type of <paramref name="reference"/>, is a supported collection type and if so returns an inserter for <paramref name="reference"/>.
        /// </summary>
        /// <parameters>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="reference">The object for which to return an enumerator if successful.</param>
        /// <param name="inserter">The inserter returned if successful.</param>
        /// <param name="itemType">The type of items the inserter can take.</param>
        /// </parameters>
        /// <returns>
        ///     True if <paramref name="referenceType"/> is a writeable collection.
        /// </returns>
        public bool IsWriteableCollection(object reference, Type referenceType, out IInserter inserter, out Type itemType)
        {
            foreach (IInserter TestInserter in CustomInserters)
            {
                Type TestType;
                if (TestInserter.TrySetReference(reference, referenceType, out TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }
            }

            foreach (IInserter TestInserter in BuiltInInserters)
            {
                Type TestType;
                if (TestInserter.TrySetReference(reference, referenceType, out TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }
            }

            inserter = null;
            itemType = null;
            return false;
        }

        /// <summary>
        ///     Checks if <paramref name="referenceType"/> is a supported collection type and if so returns the corresponding inserter.
        /// </summary>
        /// <parameters>
        /// <param name="referenceType">The type to check.</param>
        /// <param name="inserter">The inserter returned if successful.</param>
        /// <param name="itemType">The type of items the inserter can take.</param>
        /// </parameters>
        /// <returns>
        ///     True if <paramref name="referenceType"/> is a writeable collection.
        /// </returns>
        public bool IsWriteableCollection(Type referenceType, out IInserter inserter, out Type itemType)
        {
            foreach (IInserter TestInserter in CustomInserters)
            {
                Type TestType;
                if (TestInserter.TryMatchType(referenceType, out TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }
            }

            foreach (IInserter TestInserter in BuiltInInserters)
            {
                Type TestType;
                if (TestInserter.TryMatchType(referenceType, out TestType))
                {
                    inserter = TestInserter;
                    itemType = TestType;
                    return true;
                }
            }

            inserter = null;
            itemType = null;
            return false;
        }
#endregion

#region Misc
        private int SortByName(SerializedMember p1, SerializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private int SortByName(DeserializedMember p1, DeserializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private bool IsSerializableConstructor(ConstructorInfo constructor, Type serializedType, out List<SerializedMember> constructorParameters)
        {
            SerializableAttribute CustomAttribute = constructor.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomAttribute == null)
            {
                constructorParameters = null;
                return false;
            }

            if (CustomAttribute.Constructor == null)
            {
                constructorParameters = null;
                return false;
            }

            string[] Properties = CustomAttribute.Constructor.Split(',');
            ParameterInfo[] Parameters = constructor.GetParameters();
            if (Properties.Length == 0 || Properties.Length != Parameters.Length)
            {
                constructorParameters = null;
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

        private bool IsSerializableMember(object reference, Type serializedType, SerializedMember newMember)
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

        private bool IsStaticOrReadOnly(MemberInfo memberInfo)
        {
            FieldInfo AsFieldInfo;
            if ((AsFieldInfo = memberInfo as FieldInfo) != null)
            {
                if (AsFieldInfo.Attributes.HasFlag(FieldAttributes.Static) || AsFieldInfo.Attributes.HasFlag(FieldAttributes.InitOnly))
                    return true;
            }

            return false;
        }

        private bool IsExcludedFromSerialization(SerializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private bool IsReadOnlyPropertyWithNoSetter(SerializedMember newMember)
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
            }
            else
                return false;

            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Setter != null)
                    return false;
            }

            return true;
        }

        private bool IsExcludedIndexer(SerializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
                return false;

            if (newMember.MemberInfo.Name == "Item" && newMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private void CheckSerializationCondition(object reference, Type serializedType, SerializedMember newMember)
        {
            SerializableAttribute CustomSerializable = newMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Condition != null)
                {
                    MemberInfo[] ConditionMembers = serializedType.GetMember(CustomSerializable.Condition);
                    if (ConditionMembers != null)
                    {
                        foreach (MemberInfo ConditionMember in ConditionMembers)
                        {
                            FieldInfo AsFieldInfo;
                            PropertyInfo AsPropertyInfo;

                            if ((AsFieldInfo = ConditionMember as FieldInfo) != null)
                            {
                                if (AsFieldInfo.FieldType == typeof(bool))
                                    newMember.SetCondition((bool)AsFieldInfo.GetValue(reference));
                            }
                            else if ((AsPropertyInfo = ConditionMember as PropertyInfo) != null)
                            {
                                if (AsPropertyInfo.PropertyType == typeof(bool))
                                    newMember.SetCondition((bool)AsPropertyInfo.GetValue(reference));
                            }
                        }
                    }
                }
            }
        }

        private bool ListConstructorParameters(Type serializedType, out List<SerializedMember> constructorParameters)
        {
            List<ConstructorInfo> Constructors = new List<ConstructorInfo>(serializedType.GetConstructors());

            foreach (ConstructorInfo Constructor in Constructors)
                if (IsSerializableConstructor(Constructor, serializedType, out constructorParameters))
                    return true;

            constructorParameters = null;
            return false;
        }
#endregion

#region String conversions
        private byte[] String2Bytes(string s)
        {
            int CharCount;
            char[] StringChars;

            if (s == null)
                return new byte[CountByteSize] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            else
            {
                CharCount = s.Length;
                StringChars = s.ToCharArray();
            }

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

        private string Bytes2String(int count, byte[] data, int offset)
        {
            int i = offset;
            char[] StringChars = new char[count];

            for (i = 0; i < count; i++)
                StringChars[i] = BitConverter.ToChar(data, offset + (i * 2));

            return new string(StringChars);
        }

        private int FromHexDigit(byte[] data, int offset)
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

        private int FromDecimalDigit(byte[] data, int offset)
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
