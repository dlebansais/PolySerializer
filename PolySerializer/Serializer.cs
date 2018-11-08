using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PolySerializer
{
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
        SerializationFormat FileFormat { get; set; }

        /// <summary>
        ///     Serializes <see cref="root"/> and write the serialized data to <see cref="output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// </parameters>
        void Serialize(Stream output, object root);

        /// <summary>
        ///     Creates a new object from serialized content in <see cref="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        object Deserialize(Stream input);

        /// <summary>
        ///     Checks if serialized data in <see cref="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        //bool Check(Stream input);

        /// <summary>
        ///     Serializes <see cref="root"/> and write the serialized data to <see cref="output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// </parameters>
        //Task SerializeAsync(Stream output, object root);

        /// <summary>
        ///     Creates a new object from serialized content in <see cref="input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        //Task<object> DeserializeAsync(Stream input);

        /// <summary>
        ///     Checks if serialized data in <see cref="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        //Task<bool> CheckAsync(Stream input);

        /// <summary>
        ///     Returns the serialization or deserialization progress as a number between 0 and 1.
        ///     <paramref="task"/> must be one of the return values of <see cref="SerializeAsync"/> or <see cref="DeserializeAsync"/> or this method will throw an exception.
        /// </summary>
        /// <parameters>
        /// <param name="task">The serializing or deserializing task for which progress is queried.</param>
        /// </parameters>
        //double GetAsyncProgress(IAsyncResult task);

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
        ///     If not null, the stream on which the format of the serialized data has been written to in <see cref="Serialize"/>, or read from in <see cref="Deserialize"/>.
        /// </summary>
        Stream Format { get; set; }

        /// <summary>
        ///     Type of the <see cref="Root"/> object after a call to <see cref="Serialize"/>, or type of the object to create in <see cref="Deserialize"/>.
        ///     If null, <see cref="Deserialize"/> finds the type to use from the serialized data. If not null, the serialized data must be compatible with this type or <see cref="Deserialize"/> will throw an exception.
        /// </summary>
        Type RootType { get; set; }

        /// <summary>
        /// Sets or gets a list of assemblies that can override the original assembly of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; }

        /// <summary>
        /// Sets or gets a list of namespaces that can override the original namespace of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<string, string> NamespaceOverrideTable { get; set; }

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

    public delegate void SerializedHandler(object root, byte[] data);

    public class Serializer : ISerializer
    {
        #region Constants
        private const int CountByteSize = 8;
        #endregion

        #region Init
        public Serializer()
        {
        }
        #endregion

        #region Properties
        public Stream Output { get; private set; } = null;
        public object Root { get; private set; } = null;
        public Stream Input { get; private set; } = null;
        public Stream Format { get; set; } = null;
        public Type RootType { get; set; } = null;
        public IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; } = new Dictionary<Assembly, Assembly>();
        public IReadOnlyDictionary<string, string> NamespaceOverrideTable { get; set; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; } = new Dictionary<Type, Type>();
        public bool OverrideGenericArguments { get; set; } = true;
        public IReadOnlyList<IInserter> CustomInserters { get; set; } = new List<IInserter>();
        public IReadOnlyList<IInserter> BuiltInInserters { get; } = new List<IInserter>()
        {
            new ArrayInserter(),
            new ListInserter(),
            new GenericAddInserter(),
        };
        public SerializationMode Mode { get; set; } = SerializationMode.Default;
        public SerializationFormat FileFormat { get; set; } = SerializationFormat.BinaryPreferred;
        public uint MinAllocatedSize { get; set; } = 0x10000;
        public uint LastAllocatedSize { get; private set; } = 0;
        #endregion

        #region Serialization
        public void Serialize(Stream output, object root)
        {
            Output = output;
            Root = root;
            RootType = root.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            IsSerializedAsText = (FileFormat == SerializationFormat.TextPreferred) || (FileFormat == SerializationFormat.TextOnly);

            if (IsSerializedAsText)
                AddFieldStringDirect(ref Data, ref Offset, $"Mode={Mode}\n");
            else
                AddFieldInt(ref Data, ref Offset, (int)Mode);

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable(root, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize(Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            output.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;
        }

        private void Serialize(object reference, Type serializedType, long count, ref byte[] data, ref int offset, ISerializableObject nextSerialized)
        {
            if (count >= 0)
            {
                IEnumerable AsEnumerable = reference as IEnumerable;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < count; i++)
                {
                    if (i > 0 && IsSerializedAsText)
                        AddFieldStringDirect(ref data, ref offset, ";");

                    Enumerator.MoveNext();

                    object Item = Enumerator.Current;
                    ProcessSerializable(Item, ref data, ref offset);
                }
            }

            List<SerializedMember> SerializedMembers = ListSerializedMembers(reference, serializedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (SerializedMember Member in SerializedMembers)
            {
                if (MemberIndex++ > 0 && IsSerializedAsText)
                    AddFieldStringDirect(ref data, ref offset, ";");

                if (Member.Condition.HasValue)
                {
                    AddFieldBool(ref data, ref offset, Member.Condition.Value);
                    if (!Member.Condition.Value)
                        continue;

                    if (IsSerializedAsText)
                        AddFieldStringDirect(ref data, ref offset, " ");
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

                ProcessSerializable(MemberValue, ref data, ref offset);
            }

            if (nextSerialized != null)
                nextSerialized.SetSerialized();
        }

        private bool SerializeBasicType(object value, ref byte[] data, ref int offset)
        {
            Type ValueType = value.GetType();

            if (ValueType == typeof(sbyte))
                AddFieldSByte(ref data, ref offset, (sbyte)value);

            else if (ValueType == typeof(byte))
                AddFieldByte(ref data, ref offset, (byte)value);

            else if (ValueType == typeof(bool))
                AddFieldBool(ref data, ref offset, (bool)value);

            else if (ValueType == typeof(char))
                AddFieldChar(ref data, ref offset, (char)value);

            else if (ValueType == typeof(decimal))
                AddFieldDecimal(ref data, ref offset, (decimal)value);

            else if (ValueType == typeof(double))
                AddFieldDouble(ref data, ref offset, (double)value);

            else if (ValueType == typeof(float))
                AddFieldFloat(ref data, ref offset, (float)value);

            else if (ValueType == typeof(int))
                AddFieldInt(ref data, ref offset, (int)value);

            else if (ValueType == typeof(long))
                AddFieldLong(ref data, ref offset, (long)value);

            else if (ValueType == typeof(short))
                AddFieldShort(ref data, ref offset, (short)value);

            else if (ValueType == typeof(uint))
                AddFieldUInt(ref data, ref offset, (uint)value);

            else if (ValueType == typeof(ulong))
                AddFieldULong(ref data, ref offset, (ulong)value);

            else if (ValueType == typeof(ushort))
                AddFieldUShort(ref data, ref offset, (ushort)value);

            else if (ValueType == typeof(string))
                AddFieldString(ref data, ref offset, (string)value);

            else if (ValueType == typeof(Guid))
                AddFieldGuid(ref data, ref offset, (Guid)value);

            else if (ValueType.IsEnum)
            {
                Type UnderlyingSystemType = ValueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                    AddFieldSByte(ref data, ref offset, (sbyte)value);
                else if (UnderlyingSystemType == typeof(byte))
                    AddFieldByte(ref data, ref offset, (byte)value);
                else if (UnderlyingSystemType == typeof(short))
                    AddFieldShort(ref data, ref offset, (short)value);
                else if (UnderlyingSystemType == typeof(ushort))
                    AddFieldUShort(ref data, ref offset, (ushort)value);
                else if (UnderlyingSystemType == typeof(int))
                    AddFieldInt(ref data, ref offset, (int)value);
                else if (UnderlyingSystemType == typeof(uint))
                    AddFieldUInt(ref data, ref offset, (uint)value);
                else if (UnderlyingSystemType == typeof(long))
                    AddFieldLong(ref data, ref offset, (long)value);
                else if (UnderlyingSystemType == typeof(ulong))
                    AddFieldULong(ref data, ref offset, (ulong)value);
                else
                    AddFieldInt(ref data, ref offset, (int)value);
            }

            else
                return false;

            return true;
        }

        private void ProcessSerializable(object reference, ref byte[] data, ref int offset)
        {
            if (reference == null)
            {
                AddFieldNull(ref data, ref offset);
                return;
            }

            if (SerializeBasicType(reference, ref data, ref offset))
                return;

            Type ReferenceType = SerializableAncestor(reference.GetType());
            AddFieldType(ref data, ref offset, ReferenceType);

            if (ReferenceType.IsValueType)
                Serialize(reference, ReferenceType, -1, ref data, ref offset, null);
            else
            {
                if (CycleDetectionTable.ContainsKey(reference))
                {
                    long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[reference]);

                    if (IsSerializedAsText)
                        AddFieldStringDirect(ref data, ref offset, $" #{ReferenceIndex}\n");
                    else
                    {
                        AddFieldByte(ref data, ref offset, (byte)ObjectTag.ObjectIndex);
                        AddFieldLong(ref data, ref offset, ReferenceIndex);
                    }
                }
                else
                {
                    long Count = GetCollectionCount(reference);
                    if (Count < 0)
                    {
                        List<SerializedMember> ConstructorParameters;
                        if (ListConstructorParameters(reference, ReferenceType, out ConstructorParameters))
                        {
                            if (IsSerializedAsText)
                                AddFieldStringDirect(ref data, ref offset, " !");
                            else
                                AddFieldByte(ref data, ref offset, (byte)ObjectTag.ConstructedObject);

                            int ParameterIndex = 0;
                            foreach (SerializedMember Member in ConstructorParameters)
                            {
                                if (ParameterIndex++ > 0 && IsSerializedAsText)
                                    AddFieldStringDirect(ref data, ref offset, ";");

                                PropertyInfo AsPropertyInfo;
                                AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                                object MemberValue = AsPropertyInfo.GetValue(reference);

                                ProcessSerializable(MemberValue, ref data, ref offset);
                            }

                            if (IsSerializedAsText)
                                AddFieldStringDirect(ref data, ref offset, "\n");
                        }
                        else
                        {
                            if (IsSerializedAsText)
                                AddFieldStringDirect(ref data, ref offset, "\n");
                            else
                                AddFieldByte(ref data, ref offset, (byte)ObjectTag.ObjectReference);
                        }
                    }
                    else
                    {
                        if (IsSerializedAsText)
                            AddFieldStringDirect(ref data, ref offset, $" *{Count}\n");
                        else
                        {
                            AddFieldByte(ref data, ref offset, (byte)ObjectTag.ObjectList);
                            AddFieldLong(ref data, ref offset, Count);
                        }
                    }

                    AddSerializedObject(reference, Count);
                }
            }
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

        private List<SerializedMember> ListSerializedMembers(object reference, Type serializedType, ref byte[] data, ref int offset)
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
                AddFieldMembers(ref data, ref offset, SerializedMembers);

            return SerializedMembers;
        }

        private void AddFieldSByte(ref byte[] data, ref int offset, sbyte value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{((byte)value).ToString("X02")}");
            else
                AddField(ref data, ref offset, new byte[1] { (byte)value });
        }

        private void AddFieldByte(ref byte[] data, ref int offset, byte value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X02")}");
            else
                AddField(ref data, ref offset, new byte[1] { value });
        }

        private void AddFieldBool(ref byte[] data, ref int offset, bool value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"{value}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldChar(ref byte[] data, ref int offset, char value)
        {
            if (IsSerializedAsText)
            {
                if (value == '\'')
                    AddFieldStringDirect(ref data, ref offset, @"'\''");
                else
                    AddFieldStringDirect(ref data, ref offset, $"'{value}'");
            }
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldDecimal(ref byte[] data, ref int offset, decimal value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}m");
            else
            {
                int[] DecimalInts = decimal.GetBits(value);
                for (int i = 0; i < 4; i++)
                {
                    byte[] DecimalBytes = BitConverter.GetBytes(DecimalInts[i]);
                    AddField(ref data, ref offset, DecimalBytes);
                }
            }
        }

        private void AddFieldDouble(ref byte[] data, ref int offset, double value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}d");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldFloat(ref byte[] data, ref int offset, float value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"{value.ToString(CultureInfo.InvariantCulture)}f");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldInt(ref byte[] data, ref int offset, int value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X08")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldLong(ref byte[] data, ref int offset, long value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X16")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldShort(ref byte[] data, ref int offset, short value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X04")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUInt(ref byte[] data, ref int offset, uint value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X08")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldULong(ref byte[] data, ref int offset, ulong value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X16")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUShort(ref byte[] data, ref int offset, ushort value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"0x{value.ToString("X04")}");
            else
                AddField(ref data, ref offset, BitConverter.GetBytes(value));
        }

        private void AddFieldString(ref byte[] data, ref int offset, string value)
        {
            if (IsSerializedAsText)
            {
                if (value == null)
                    value = "null";
                else
                    value = "\"" + value.Replace("\"", "\\\"") + "\"";

                AddField(ref data, ref offset, Encoding.UTF8.GetBytes(value));
            }
            else
                AddField(ref data, ref offset, String2Bytes(value));
        }

        private void AddFieldGuid(ref byte[] data, ref int offset, Guid value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, value.ToString("B"));
            else
                AddField(ref data, ref offset, value.ToByteArray());
        }

        private void AddFieldNull(ref byte[] data, ref int offset)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, "null");
            else
                AddField(ref data, ref offset, new byte[CountByteSize] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        }

        private void AddFieldType(ref byte[] data, ref int offset, Type value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(ref data, ref offset, $"{{{value.AssemblyQualifiedName}}}");
            else
                AddFieldString(ref data, ref offset, value.AssemblyQualifiedName);
        }

        private void AddFieldStringDirect(ref byte[] data, ref int offset, string s)
        {
            AddField(ref data, ref offset, Encoding.UTF8.GetBytes(s));
        }

        private void AddFieldMembers(ref byte[] data, ref int offset, List<SerializedMember> serializedMembers)
        {
            if (IsSerializedAsText)
            {
                for (int i = 0; i < serializedMembers.Count; i++)
                {
                    if (i > 0)
                        AddFieldStringDirect(ref data, ref offset, ",");

                    SerializedMember Member = serializedMembers[i];
                    AddFieldStringDirect(ref data, ref offset, Member.MemberInfo.Name);
                }

                AddFieldStringDirect(ref data, ref offset, "\n");
            }
            else
            {
                AddFieldInt(ref data, ref offset, serializedMembers.Count);

                foreach (SerializedMember Member in serializedMembers)
                    AddFieldString(ref data, ref offset, Member.MemberInfo.Name);
            }
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

        private bool IsSerializedAsText;
        private List<ISerializableObject> SerializedObjectList = new List<ISerializableObject>();
        private Dictionary<object, SerializableObject> CycleDetectionTable = new Dictionary<object, SerializableObject>();
        #endregion

        #region Deserialization
        public object Deserialize(Stream input)
        {
            Input = input;

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            ReadField(ref Data, ref Offset, 4);

            if (FileFormat == SerializationFormat.TextPreferred || FileFormat == SerializationFormat.BinaryPreferred)
                IsDeserializedAsText = (Data[0] == 'M' && Data[1] == 'o' && Data[2] == 'd' && Data[3] == 'e');
            else
                IsDeserializedAsText = (FileFormat == SerializationFormat.TextOnly);

            if (IsDeserializedAsText)
            {
                Offset += 4;

                ReadField(ref Data, ref Offset, 8);
                string s = Encoding.UTF8.GetString(Data, Offset, 8).Substring(1, 7);

                if (s == SerializationMode.Default.ToString())
                {
                    Mode = SerializationMode.Default;
                    Offset += 9;
                }
                else if (s == SerializationMode.MemberName.ToString().Substring(0, 7))
                {
                    Mode = SerializationMode.MemberName;
                    Offset += 12;
                }
                else if (s == SerializationMode.MemberOrder.ToString().Substring(0, 7))
                {
                    Mode = SerializationMode.MemberOrder;
                    Offset += 13;
                }
                else
                    throw new InvalidDataException("Mode");
            }
            else
            {
                Mode = (SerializationMode)BitConverter.ToInt32(Data, Offset);
                Offset += 4;
            }

            DeserializedObjectList.Clear();

            object Reference;
            ProcessDeserializable(RootType, ref Data, ref Offset, out Reference);

            Root = Reference;

            if (RootType == null)
                RootType = Root.GetType();

            int i = 0;
            while (i < DeserializedObjectList.Count)
            {
                IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
                Reference = NextDeserialized.Reference;
                Deserialize(ref Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref Data, ref Offset, NextDeserialized);
            }

            return Root;
        }

        private void Deserialize(ref object reference, Type referenceType, long count, ref byte[] data, ref int offset, IDeserializedObject nextDeserialized)
        {
            if (count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(reference, referenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (i > 0 && IsDeserializedAsText)
                        {
                            ReadField(ref data, ref offset, 1);
                            char c = (char)data[offset];
                            offset++;
                        }

                        object Item;
                        ProcessDeserializable(ItemType, ref data, ref offset, out Item);

                        Inserter.AddItem(Item);
                    }
                }
            }

            Type DeserializedType = SerializableAncestor(referenceType);
            List<DeserializedMember> DeserializedMembers = ListDeserializedMembers(DeserializedType, ref data, ref offset);

            int MemberIndex = 0;
            foreach (DeserializedMember Member in DeserializedMembers)
            {
                if (MemberIndex++ > 0 && IsDeserializedAsText)
                {
                    ReadField(ref data, ref offset, 1);
                    char c = (char)data[offset];
                    offset++;
                }

                if (Member.HasCondition)
                {
                    bool ConditionValue = ReadFieldBool(ref data, ref offset);
                    if (!ConditionValue)
                        continue;

                    if (IsDeserializedAsText)
                    {
                        ReadField(ref data, ref offset, 1);
                        offset++;
                    }
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

                ProcessDeserializable(MemberType, ref data, ref offset, out MemberValue);

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

        private bool DeserializeBasicType(Type valueType, ref byte[] data, ref int offset, out object value)
        {
            if (valueType == typeof(sbyte))
                value = ReadFieldSByte(ref data, ref offset);

            else if (valueType == typeof(byte))
                value = ReadFieldByte(ref data, ref offset);

            else if (valueType == typeof(bool))
                value = ReadFieldBool(ref data, ref offset);

            else if (valueType == typeof(char))
                value = ReadFieldChar(ref data, ref offset);

            else if (valueType == typeof(decimal))
                value = ReadFieldDecimal(ref data, ref offset);

            else if (valueType == typeof(double))
                value = ReadFieldDouble(ref data, ref offset);

            else if (valueType == typeof(float))
                value = ReadFieldFloat(ref data, ref offset);

            else if (valueType == typeof(int))
                value = ReadFieldInt(ref data, ref offset);

            else if (valueType == typeof(long))
                value = ReadFieldLong(ref data, ref offset);

            else if (valueType == typeof(short))
                value = ReadFieldShort(ref data, ref offset);

            else if (valueType == typeof(uint))
                value = ReadFieldUInt(ref data, ref offset);

            else if (valueType == typeof(ulong))
                value = ReadFieldULong(ref data, ref offset);

            else if (valueType == typeof(ushort))
                value = ReadFieldUShort(ref data, ref offset);

            else if (valueType == typeof(string))
                value = ReadFieldString(ref data, ref offset);
            
            else if (valueType == typeof(Guid))
                value = ReadFieldGuid(ref data, ref offset);

            else if (valueType != null && valueType.IsEnum)
            {
                Type UnderlyingSystemType = valueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                    value = Enum.ToObject(valueType, ReadFieldSByte(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(byte))
                    value = Enum.ToObject(valueType, ReadFieldByte(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(short))
                    value = Enum.ToObject(valueType, ReadFieldShort(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(ushort))
                    value = Enum.ToObject(valueType, ReadFieldUShort(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(int))
                    value = Enum.ToObject(valueType, ReadFieldInt(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(uint))
                    value = Enum.ToObject(valueType, ReadFieldUInt(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(long))
                    value = Enum.ToObject(valueType, ReadFieldLong(ref data, ref offset));
                else if (UnderlyingSystemType == typeof(ulong))
                    value = Enum.ToObject(valueType, ReadFieldULong(ref data, ref offset));
                else
                    value = Enum.ToObject(valueType, ReadFieldInt(ref data, ref offset));
            }

            else
            {
                value = null;
                return false;
            }

            return true;
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

        private void ProcessDeserializable(Type referenceType, ref byte[] data, ref int offset, out object reference)
        {
            if (DeserializeBasicType(referenceType, ref data, ref offset, out reference))
                return;

            string ReferenceTypeName = ReadFieldType(ref data, ref offset);
            if (ReferenceTypeName == null)
            {
                reference = null;
                return;
            }

            referenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = referenceType;
            OverrideType(ref referenceType);
            Type NewType = referenceType;
            referenceType = OriginalType;

            if (referenceType.IsValueType)
            {
                CreateObject(NewType, ref reference);
                Deserialize(ref reference, referenceType, -1, ref data, ref offset, null);
            }

            else
            {
                ObjectTag ReferenceTag = ReadFieldTag(ref data, ref offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    int ReferenceIndex = ReadFieldObjectIndex(ref data, ref offset);
                    reference = DeserializedObjectList[ReferenceIndex].Reference;
                }

                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    CreateObject(NewType, ref reference);
                    AddDeserializedObject(reference, referenceType, -1);
                }

                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount(ref data, ref offset);

                    CreateObject(NewType, Count, ref reference);
                    AddDeserializedObject(reference, referenceType, Count);
                }

                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(reference, referenceType, out ConstructorParameters))
                    {
                        object[] Parameters = new object[ConstructorParameters.Count];

                        for (int i = 0; i < Parameters.Length; i++)
                        {
                            if (i > 0 && IsDeserializedAsText)
                            {
                                ReadField(ref data, ref offset, 1);
                                char c = (char)data[offset];
                                offset++;
                            }

                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            object MemberValue;
                            Type MemberType = AsPropertyInfo.PropertyType;
                            ProcessDeserializable(MemberType, ref data, ref offset, out MemberValue);

                            Parameters[i] = MemberValue;
                        }

                        if (IsDeserializedAsText)
                        {
                            ReadField(ref data, ref offset, 1);
                            char c = (char)data[offset];
                            offset++;
                        }

                        CreateObject(NewType, Parameters, ref reference);
                        AddDeserializedObject(reference, referenceType, -1);
                    }
                }
            }
        }

        private bool OverrideType(ref Type referenceType)
        {
            if (TypeOverrideTable.Count == 0 && AssemblyOverrideTable.Count == 0 && NamespaceOverrideTable.Count == 0)
                return false;

            if (TypeOverrideTable.Count > 0)
            {
                if (OverrideDirectType(ref referenceType))
                    return true;

                if (OverrideGenericDefinitionType(ref referenceType))
                    return true;
            }
            
            if (AssemblyOverrideTable.Count > 0 || NamespaceOverrideTable.Count > 0)
            {
                Type[] TypeList;
                if (referenceType.IsGenericType && !referenceType.IsGenericTypeDefinition)
                {
                    Type[] GenericArguments = referenceType.GetGenericArguments();
                    TypeList = new Type[1 + GenericArguments.Length];
                    TypeList[0] = referenceType.GetGenericTypeDefinition();
                    for (int i = 0; i < GenericArguments.Length; i++)
                        TypeList[i + 1] = GenericArguments[i];
                }
                else
                {
                    TypeList = new Type[1];
                    TypeList[0] = referenceType;
                }

                bool GlobalOverride = false;

                for (int i = 0; i < TypeList.Length; i++)
                {
                    if (!OverrideGenericArguments && i > 0)
                        break;

                    Type Type = TypeList[i];
                    bool Override = false;

                    Assembly Assembly = Type.Assembly;
                    if (AssemblyOverrideTable.ContainsKey(Assembly))
                    {
                        Assembly = AssemblyOverrideTable[Assembly];
                        Override = true;
                    }

                    string TypeName = null;
                    string[] NamePath = Type.FullName.Split('.');

                    for (int j = NamePath.Length; j > 0; j--)
                    {
                        string NameSpace = "";
                        for (int k = 0; k + 1 < j; k++)
                        {
                            if (NameSpace.Length > 0)
                                NameSpace += ".";
                            NameSpace += NamePath[k];
                        }

                        if (NamespaceOverrideTable.ContainsKey(NameSpace))
                        {
                            NameSpace = NamespaceOverrideTable[NameSpace];
                            TypeName = NameSpace + "." + NamePath[NamePath.Length - 1];
                            Override = true;
                            break;
                        }
                    }

                    if (TypeName == null)
                        TypeName = Type.FullName;

                    if (Override)
                    {
                        GlobalOverride = true;
                        Type = Assembly.GetType(TypeName);
                        if (Type != null)
                            TypeList[i] = Type;
                    }
                }

                if (GlobalOverride)
                {
                    if (TypeList.Length == 1)
                        referenceType = TypeList[0];
                    else
                    {
                        Type[] GenericArguments = new Type[TypeList.Length - 1];
                        for (int i = 1; i < TypeList.Length; i++)
                            GenericArguments[i - 1] = TypeList[i];
                        referenceType = TypeList[0].MakeGenericType(GenericArguments);
                    }

                    return true;
                }
            }

            return false;
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

        private void AddDeserializedObject(object Reference, Type DeserializedType, long Count)
        {
            DeserializedObjectList.Add(new DeserializedObject(Reference, DeserializedType, Count));
        }

        private List<DeserializedMember> ListDeserializedMembers(Type deserializedType, ref byte[] data, ref int offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                List<string> MemberNames = ReadFieldMembers(ref data, ref offset);
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

        private sbyte ReadFieldSByte(ref byte[] data, ref int offset)
        {
            sbyte Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 4);

                uint n = 0;
                for (int i = 0; i < 2; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = (sbyte)n;
                offset += 4;
            }
            else
            {
                ReadField(ref data, ref offset, 1);
                Value = (sbyte)data[offset];
                offset++;
            }

            return Value;
        }

        private byte ReadFieldByte(ref byte[] data, ref int offset)
        {
            byte Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 4);

                uint n = 0;
                for (int i = 0; i < 2; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = (byte)n;
                offset += 4;
            }
            else
            {
                ReadField(ref data, ref offset, 1);
                Value = data[offset];
                offset++;
            }

            return Value;
        }

        private bool ReadFieldBool(ref byte[] data, ref int offset)
        {
            bool Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 4);

                Value = (data[offset + 0] == 'T' && data[offset + 1] == 'r' && data[offset + 2] == 'u' && data[offset + 3] == 'e');
                offset += 4;

                if (!Value)
                {
                    ReadField(ref data, ref offset, 1);
                    offset++;
                }
            }
            else
            {
                ReadField(ref data, ref offset, 1);
                Value = BitConverter.ToBoolean(data, offset);
                offset += 1;
            }

            return Value;
        }

        private char ReadFieldChar(ref byte[] data, ref int offset)
        {
            char Value;

            if (IsSerializedAsText)
            {
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
            }
            else
            {
                ReadField(ref data, ref offset, 2);
                Value = BitConverter.ToChar(data, offset);
                offset += 2;
            }

            return Value;
        }

        private decimal ReadFieldDecimal(ref byte[] data, ref int offset)
        {
            decimal Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;
                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != 'm');

                string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Parsed))
                    Value = Parsed;
                else
                    Value = default(decimal);
            }
            else
            {
                ReadField(ref data, ref offset, 16);
                int[] DecimalInts = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    DecimalInts[i] = BitConverter.ToInt32(data, offset);
                    offset += 4;
                }

                Value = new decimal(DecimalInts);
            }

            return Value;
        }

        private double ReadFieldDouble(ref byte[] data, ref int offset)
        {
            double Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;
                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != 'd');

                string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
                if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out double Parsed))
                    Value = Parsed;
                else
                    Value = default(double);
            }
            else
            {
                ReadField(ref data, ref offset, 8);
                Value = BitConverter.ToDouble(data, offset);
                offset += 8;
            }

            return Value;
        }

        private float ReadFieldFloat(ref byte[] data, ref int offset)
        {
            float Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;
                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != 'f');

                string s = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
                if (float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out float Parsed))
                    Value = Parsed;
                else
                    Value = default(float);
            }
            else
            {
                ReadField(ref data, ref offset, 4);
                Value = BitConverter.ToSingle(data, offset);
                offset += 4;
            }

            return Value;
        }

        private int ReadFieldInt(ref byte[] data, ref int offset)
        {
            int Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 10);

                uint n = 0;
                for (int i = 0; i < 8; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = (int)n;
                offset += 10;
            }
            else
            {
                ReadField(ref data, ref offset, 4);
                Value = BitConverter.ToInt32(data, offset);
                offset += 4;
            }

            return Value;
        }

        private long ReadFieldLong(ref byte[] data, ref int offset)
        {
            long Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 18);

                ulong n = 0;
                for (int i = 0; i < 16; i++)
                    n = (n * 16) + (ulong)FromHexDigit(data, offset + 2 + i);

                Value = (long)n;
                offset += 18;
            }
            else
            {
                ReadField(ref data, ref offset, 8);
                Value = BitConverter.ToInt64(data, offset);
                offset += 8;
            }

            return Value;
        }

        private short ReadFieldShort(ref byte[] data, ref int offset)
        {
            short Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 6);

                uint n = 0;
                for (int i = 0; i < 4; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = (short)n;
                offset += 6;
            }
            else
            {
                ReadField(ref data, ref offset, 2);
                Value = BitConverter.ToInt16(data, offset);
                offset += 2;
            }

            return Value;
        }

        private uint ReadFieldUInt(ref byte[] data, ref int offset)
        {
            uint Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 10);

                uint n = 0;
                for (int i = 0; i < 8; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = n;
                offset += 10;
            }
            else
            {
                ReadField(ref data, ref offset, 4);
                Value = BitConverter.ToUInt32(data, offset);
                offset += 4;
            }

            return Value;
        }

        private ulong ReadFieldULong(ref byte[] data, ref int offset)
        {
            ulong Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 18);

                ulong n = 0;
                for (int i = 0; i < 16; i++)
                    n = (n * 16) + (ulong)FromHexDigit(data, offset + 2 + i);

                Value = n;
                offset += 18;
            }
            else
            {
                ReadField(ref data, ref offset, 8);
                Value = BitConverter.ToUInt64(data, offset);
                offset += 8;
            }

            return Value;
        }

        private ushort ReadFieldUShort(ref byte[] data, ref int offset)
        {
            ushort Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 6);

                uint n = 0;
                for (int i = 0; i < 4; i++)
                    n = (n * 16) + (uint)FromHexDigit(data, offset + 2 + i);

                Value = (ushort)n;
                offset += 6;
            }
            else
            {
                ReadField(ref data, ref offset, 2);
                Value = BitConverter.ToUInt16(data, offset);
                offset += 2;
            }

            return Value;
        }

        private string ReadFieldString(ref byte[] data, ref int offset)
        {
            string Value;

            if (IsSerializedAsText)
            {
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

                for (; ; )
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
            }
            else
            {
                string StringValue;
                ReadStringField(ref data, ref offset, out StringValue);
                Value = StringValue;
            }

            return Value;
        }

        private Guid ReadFieldGuid(ref byte[] data, ref int offset)
        {
            Guid Value;

            if (IsSerializedAsText)
            {
                ReadField(ref data, ref offset, 38);
                string Content = Encoding.UTF8.GetString(data, offset, 38);
                offset += 38;

                if (Guid.TryParse(Content, out Guid AsGuid))
                    Value = AsGuid;
                else
                    Value = Guid.Empty;
            }
            else
            {
                ReadField(ref data, ref offset, 16);
                byte[] GuidBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                    GuidBytes[i] = data[offset++];
                Value = new Guid(GuidBytes);
                //Value = Guid.NewGuid();
            }

            return Value;
        }

        private string ReadFieldType(ref byte[] data, ref int offset)
        {
            string Value;

            if (IsSerializedAsText)
            {
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
            }
            else
            {
                ReadStringField(ref data, ref offset, out string AsString);
                Value = AsString;
            }

            return Value;
        }

        private List<string> ReadFieldMembers(ref byte[] data, ref int offset)
        {
            List<string> MemberNames;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;

                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != '\n');

                string AllNames = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
                string[] Splitted = AllNames.Split(',');
                MemberNames = new List<string>(Splitted);
            }
            else
            {
                MemberNames = new List<string>();

                ReadField(ref data, ref offset, 4);
                int MemberCount = BitConverter.ToInt32(data, offset);
                offset += 4;

                for (int i = 0; i < MemberCount; i++)
                {
                    string MemberName;
                    ReadStringField(ref data, ref offset, out MemberName);
                    MemberNames.Add(MemberName);
                }
            }

            return MemberNames;
        }

        private ObjectTag ReadFieldTag(ref byte[] data, ref int offset)
        {
            ObjectTag Value;

            if (IsSerializedAsText)
            {
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
            }
            else
            {
                ReadField(ref data, ref offset, 1);
                Value = (ObjectTag)data[offset++];
            }

            return Value;
        }

        private int ReadFieldObjectIndex(ref byte[] data, ref int offset)
        {
            int Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;
                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != '\n');

                int n = 0;
                for (int i = BaseOffset; i + 1 < offset; i++)
                    n = (n * 10) + FromDecimalDigit(data, i);

                Value = n;
            }
            else
                Value = (int)ReadFieldLong(ref data, ref offset);

            return Value;
        }

        private long ReadFieldCount(ref byte[] data, ref int offset)
        {
            long Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = offset;
                do
                    ReadField(ref data, ref offset, 1);
                while (data[offset++] != '\n');

                long n = 0;
                for (int i = BaseOffset; i + 1 < offset; i++)
                    n = (n * 10) + FromDecimalDigit(data, i);

                Value = n;
            }
            else
                Value = ReadFieldLong(ref data, ref offset);

            return Value;
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
                long Length = (Input.Length - Input.Position);
                if (Length > data.Length - offset)
                    Length = data.Length - offset;

                Input.Read(data, offset, (int)Length);
                offset = 0;
            }
        }

        private bool IsDeserializedAsText;
        private List<IDeserializedObject> DeserializedObjectList = new List<IDeserializedObject>();
        #endregion

        #region Check
        public bool Check(Stream Input)
        {
            return false;
        }
        #endregion

        #region Tools
        public static bool IsReadableCollection(Type t, object reference, out IEnumerator enumerator)
        {
            Type CurrentType;

            if (t.IsGenericType)
                CurrentType = t.GetGenericTypeDefinition();
            else
                CurrentType = t;

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
        #endregion

        #region Misc
        public static Type SerializableAncestor(Type referenceType)
        {
            Type t = referenceType;

            while (t != null && !t.Attributes.HasFlag(TypeAttributes.Serializable) && t.GetCustomAttribute(typeof(PolySerializer.SerializableAttribute)) == null)
                t = t.BaseType;

            return t;
        }

        private int SortByName(SerializedMember p1, SerializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private int SortByName(DeserializedMember p1, DeserializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private bool IsSerializableConstructor(ConstructorInfo constructor, object reference, Type serializedType, out List<SerializedMember> constructorParameters)
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

        private bool ListConstructorParameters(object reference, Type serializedType, out List<SerializedMember> constructorParameters)
        {
            List<ConstructorInfo> Constructors = new List<ConstructorInfo>(serializedType.GetConstructors());

            foreach (ConstructorInfo Constructor in Constructors)
                if (IsSerializableConstructor(Constructor, reference, serializedType, out constructorParameters))
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
                StringBytes[CountByteSize + i * 2 + 0] = Content[0];
                StringBytes[CountByteSize + i * 2 + 1] = Content[1];
            }

            return StringBytes;
        }

        private string Bytes2String(int count, byte[] data, int offset)
        {
            int i = offset;
            char[] StringChars = new char[count];

            for (i = 0; i < count; i++)
                StringChars[i] = BitConverter.ToChar(data, offset + i * 2);

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
