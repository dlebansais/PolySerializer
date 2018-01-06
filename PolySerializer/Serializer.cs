using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        ///     Serializes <see cref="Root"/> and write the serialized data to <see cref="Output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Output">Stream receiving the serialized data.</param>
        /// <param name="Root">Serialized object.</param>
        /// </parameters>
        void Serialize(Stream Output, object Root);

        /// <summary>
        ///     Creates a new object from serialized content in <see cref="Input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        object Deserialize(Stream Input);

        /// <summary>
        ///     Checks if serialized data in <see cref="Input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        //bool Check(Stream Input);

        /// <summary>
        ///     Returns a human-readable description of the serializable part of <see cref="Root"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Root">Serialized object.</param>
        /// </parameters>
        //string Print(object Root);

        /// <summary>
        ///     Returns a human-readable description of the serialized content in <see cref="Input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to be printed.</param>
        /// </parameters>
        //string Print(Stream Input);
        /// <summary>
        ///     Serializes <see cref="Root"/> and write the serialized data to <see cref="Output"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Output">Stream receiving the serialized data.</param>
        /// <param name="Root">Serialized object.</param>
        /// </parameters>
        //Task SerializeAsync(Stream Output, object Root);

        /// <summary>
        ///     Creates a new object from serialized content in <see cref="Input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to create the new object.</param>
        /// </parameters>
        //Task<object> DeserializeAsync(Stream Input);

        /// <summary>
        ///     Checks if serialized data in <see cref="Input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to check for compatibility.</param>
        /// </parameters>
        //Task<bool> CheckAsync(Stream Input);

        /// <summary>
        ///     Returns a human-readable description of the serializable part of <see cref="Root"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Root">Serialized object.</param>
        /// </parameters>
        //Task<string> PrintAsync(object Root);

        /// <summary>
        ///     Returns a human-readable description of the serialized content in <see cref="Input"/>.
        /// </summary>
        /// <parameters>
        /// <param name="Input">Stream from which serialized data is read to be printed.</param>
        /// </parameters>
        //Task<string> PrintAsync(Stream Input);

        /// <summary>
        ///     Returns the serialization or deserialization progress as a number between 0 and 1.
        ///     <see cref="Task"/> must be one of the return values of <see cref="SerializeAsync"/> or <see cref="DeserializeAsync"/> or this method will throw an exception.
        /// </summary>
        /// <parameters>
        /// <param name="Task">The serializing or deserializing task for which progress is queried.</param>
        /// </parameters>
        //double GetAsyncProgress(IAsyncResult Task);

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

    public delegate void SerializedHandler(object Root, byte[] Data);

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
        public uint MinAllocatedSize { get; set; } = 0x10000;
        public uint LastAllocatedSize { get; private set; } = 0;
        private Dictionary<object, SerializableObject> CycleDetectionTable = new Dictionary<object, SerializableObject>();
        #endregion

        #region Serialization
        public void Serialize(Stream Output, object Root)
        {
            this.Output = Output;
            this.Root = Root;
            RootType = Root.GetType();

            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Mode));

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable(Root, ref Data, ref Offset);

            int i = 0;
            while (i < SerializedObjectList.Count)
            {
                ISerializableObject NextSerialized = SerializedObjectList[i++];
                object Reference = NextSerialized.Reference;
                Serialize(Output, Reference, NextSerialized.ReferenceType, NextSerialized.Count, ref Data, ref Offset, NextSerialized);
            }

            Output.Write(Data, 0, Offset);
            LastAllocatedSize = (uint)Data.Length;
        }

        private void Serialize(Stream Output, object Reference, Type SerializedType, long Count, ref byte[] Data, ref int Offset, ISerializableObject NextSerialized)
        {
            if (Count >= 0)
            {
                IEnumerable AsEnumerable = Reference as IEnumerable;
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                for (long i = 0; i < Count; i++)
                {
                    Enumerator.MoveNext();

                    object Item = Enumerator.Current;
                    ProcessSerializable(Item, ref Data, ref Offset);
                }
            }

            List<SerializedMember> SerializedMembers = ListSerializedMembers(Reference, SerializedType, ref Data, ref Offset);

            foreach (SerializedMember Member in SerializedMembers)
            {
                if (Member.Condition.HasValue)
                {
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(Member.Condition.Value));
                    if (!Member.Condition.Value)
                        continue;
                }

                object MemberValue;

                FieldInfo AsFieldInfo;
                PropertyInfo AsPropertyInfo;

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    MemberValue = AsFieldInfo.GetValue(Reference);

                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    MemberValue = AsPropertyInfo.GetValue(Reference);
                }

                ProcessSerializable(MemberValue, ref Data, ref Offset);
            }

            if (NextSerialized != null)
                NextSerialized.SetSerialized();
        }

        private bool SerializeBasicType(object Value, ref byte[] Data, ref int Offset)
        {
            Type ValueType = Value.GetType();

            if (ValueType == typeof(sbyte))
                AddField(Output, ref Data, ref Offset, new byte[1] { (byte)(sbyte)Value });

            else if (ValueType == typeof(byte))
                AddField(Output, ref Data, ref Offset, new byte[1] { (byte)Value });

            else if (ValueType == typeof(bool))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((bool)Value));

            else if (ValueType == typeof(char))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((char)Value));

            else if (ValueType == typeof(decimal))
            {
                int[] DecimalInts = decimal.GetBits((decimal)Value);
                for (int i = 0; i < 4; i++)
                {
                    byte[] DecimalBytes = BitConverter.GetBytes(DecimalInts[i]);
                    AddField(Output, ref Data, ref Offset, DecimalBytes);
                }
            }

            else if (ValueType == typeof(double))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((double)Value));

            else if (ValueType == typeof(float))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((float)Value));

            else if (ValueType == typeof(int))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Value));

            else if (ValueType == typeof(long))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((long)Value));

            else if (ValueType == typeof(short))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((short)Value));

            else if (ValueType == typeof(uint))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((uint)Value));

            else if (ValueType == typeof(ulong))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((ulong)Value));

            else if (ValueType == typeof(ushort))
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((ushort)Value));

            else if (ValueType == typeof(string))
                AddField(Output, ref Data, ref Offset, String2Bytes((string)Value));

            else if (ValueType == typeof(Guid))
                AddField(Output, ref Data, ref Offset, ((Guid)Value).ToByteArray());

            else if (ValueType.IsEnum)
            {
                Type UnderlyingSystemType = ValueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((sbyte)Value));
                else if (UnderlyingSystemType == typeof(byte))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((byte)Value));
                else if (UnderlyingSystemType == typeof(short))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((short)Value));
                else if (UnderlyingSystemType == typeof(ushort))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((ushort)Value));
                else if (UnderlyingSystemType == typeof(int))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Value));
                else if (UnderlyingSystemType == typeof(uint))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((uint)Value));
                else if (UnderlyingSystemType == typeof(long))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((long)Value));
                else if (UnderlyingSystemType == typeof(ulong))
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((ulong)Value));
                else
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Value));
            }

            else
                return false;

            return true;
        }

        private void ProcessSerializable(object Reference, ref byte[] Data, ref int Offset)
        {
            if (Reference == null)
            {
                AddField(Output, ref Data, ref Offset, new byte[CountByteSize] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
                return;
            }

            if (SerializeBasicType(Reference, ref Data, ref Offset))
                return;

            Type ReferenceType = SerializableAncestor(Reference.GetType());
            AddField(Output, ref Data, ref Offset, String2Bytes(ReferenceType.AssemblyQualifiedName));

            if (ReferenceType.IsValueType)
                Serialize(Output, Reference, ReferenceType, -1, ref Data, ref Offset, null);

            else
            {
                if (CycleDetectionTable.ContainsKey(Reference))
                {
                    AddField(Output, ref Data, ref Offset, new byte[1] { (byte)ObjectTag.ObjectIndex });
                    long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[Reference]);
                    AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(ReferenceIndex));
                }
                else
                {
                    long Count = GetCollectionCount(Reference);
                    if (Count < 0)
                    {
                        List<SerializedMember> ConstructorParameters;
                        if (ListConstructorParameters(Reference, ReferenceType, out ConstructorParameters))
                        {
                            AddField(Output, ref Data, ref Offset, new byte[1] { (byte)ObjectTag.ConstructedObject });

                            foreach (SerializedMember Member in ConstructorParameters)
                            {
                                PropertyInfo AsPropertyInfo;
                                AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                                object MemberValue = AsPropertyInfo.GetValue(Reference);

                                ProcessSerializable(MemberValue, ref Data, ref Offset);
                            }
                        }
                        else
                            AddField(Output, ref Data, ref Offset, new byte[1] { (byte)ObjectTag.ObjectReference });
                    }
                    else
                    {
                        AddField(Output, ref Data, ref Offset, new byte[1] { (byte)ObjectTag.ObjectList });
                        AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(Count));
                    }

                    AddSerializedObject(Reference, Count);
                }
            }
        }

        private List<ISerializableObject> SerializedObjectList = new List<ISerializableObject>();
        #endregion

        #region Deserialization
        public object Deserialize(Stream Input)
        {
            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            ReadField(Input, ref Data, ref Offset, 4);
            Mode = (SerializationMode)BitConverter.ToInt32(Data, Offset);
            Offset += 4;

            DeserializedObjectList.Clear();

            object Reference;
            ProcessDeserializable(Input, RootType, ref Data, ref Offset, out Reference);

            Root = Reference;

            if (RootType == null)
                RootType = Root.GetType();

            int i = 0;
            while (i < DeserializedObjectList.Count)
            {
                IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
                Reference = NextDeserialized.Reference;
                Deserialize(Input, ref Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref Data, ref Offset, NextDeserialized);
            }

            return Root;
        }

        private void Deserialize(Stream Input, ref object Reference, Type ReferenceType, long Count, ref byte[] Data, ref int Offset, IDeserializedObject NextDeserialized)
        {
            if (Count >= 0)
            {
                IInserter Inserter;
                Type ItemType;
                if (IsWriteableCollection(Reference, ReferenceType, out Inserter, out ItemType))
                {
                    for (long i = 0; i < Count; i++)
                    {
                        object Item;
                        ProcessDeserializable(Input, ItemType, ref Data, ref Offset, out Item);

                        Inserter.AddItem(Item);
                    }
                }
            }

            Type DeserializedType = SerializableAncestor(ReferenceType);
            List<DeserializedMember> DeserializedMembers = ListDeserializedMembers(Input, DeserializedType, ref Data, ref Offset);

            foreach (DeserializedMember Member in DeserializedMembers)
            {
                if (Member.HasCondition)
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    bool ConditionValue = BitConverter.ToBoolean(Data, Offset);
                    Offset++;

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

                ProcessDeserializable(Input, MemberType, ref Data, ref Offset, out MemberValue);

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                    AsFieldInfo.SetValue(Reference, MemberValue);

                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    if (Member.PropertySetter == null)
                        AsPropertyInfo.SetValue(Reference, MemberValue);
                    else
                        Member.PropertySetter.Invoke(Reference, new object[] { MemberValue });
                }
            }

            if (NextDeserialized != null)
                NextDeserialized.SetDeserialized();
        }

        private bool DeserializeBasicType(Stream Input, Type ValueType, ref byte[] Data, ref int Offset, out object Value)
        {
            if (ValueType == typeof(sbyte))
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = (sbyte)Data[Offset];
                Offset++;
            }

            else if (ValueType == typeof(byte))
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = Data[Offset];
                Offset++;
            }

            else if (ValueType == typeof(bool))
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = BitConverter.ToBoolean(Data, Offset);
                Offset += 1;
            }

            else if (ValueType == typeof(char))
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToChar(Data, Offset);
                Offset += 2;
            }

            else if (ValueType == typeof(decimal))
            {
                ReadField(Input, ref Data, ref Offset, 16);
                int[] DecimalInts = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    DecimalInts[i] = BitConverter.ToInt32(Data, Offset);
                    Offset += 4;
                }

                Value = new decimal(DecimalInts);
            }

            else if (ValueType == typeof(double))
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToDouble(Data, Offset);
                Offset += 8;
            }

            else if (ValueType == typeof(float))
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToSingle(Data, Offset);
                Offset += 4;
            }

            else if (ValueType == typeof(int))
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToInt32(Data, Offset);
                Offset += 4;
            }

            else if (ValueType == typeof(long))
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToInt64(Data, Offset);
                Offset += 8;
            }

            else if (ValueType == typeof(short))
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToInt16(Data, Offset);
                Offset += 2;
            }

            else if (ValueType == typeof(uint))
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToUInt32(Data, Offset);
                Offset += 4;
            }

            else if (ValueType == typeof(ulong))
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToUInt64(Data, Offset);
                Offset += 8;
            }

            else if (ValueType == typeof(ushort))
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToUInt16(Data, Offset);
                Offset += 2;
            }

            else if (ValueType == typeof(string))
            {
                string StringValue;
                ReadStringField(Input, ref Data, ref Offset, out StringValue);
                Value = StringValue;
            }
            
            else if (ValueType == typeof(Guid))
            {
                ReadField(Input, ref Data, ref Offset, 16);
                byte[] GuidBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                    GuidBytes[i] = Data[Offset++];
                Value = new Guid(GuidBytes);
                //Value = Guid.NewGuid();
            }

            else if (ValueType != null && ValueType.IsEnum)
            {
                Type UnderlyingSystemType = ValueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    Value = Enum.ToObject(ValueType, (sbyte)Data[Offset++]);
                }
                else if (UnderlyingSystemType == typeof(byte))
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    Value = Enum.ToObject(ValueType, Data[Offset++]);
                }
                else if (UnderlyingSystemType == typeof(short))
                {
                    ReadField(Input, ref Data, ref Offset, 2);
                    Value = Enum.ToObject(ValueType, BitConverter.ToInt16(Data, Offset));
                    Offset += 2;
                }
                else if (UnderlyingSystemType == typeof(ushort))
                {
                    ReadField(Input, ref Data, ref Offset, 2);
                    Value = Enum.ToObject(ValueType, BitConverter.ToUInt16(Data, Offset));
                    Offset += 2;
                }
                else if (UnderlyingSystemType == typeof(int))
                {
                    ReadField(Input, ref Data, ref Offset, 4);
                    Value = Enum.ToObject(ValueType, BitConverter.ToInt32(Data, Offset));
                    Offset += 4;
                }
                else if (UnderlyingSystemType == typeof(uint))
                {
                    ReadField(Input, ref Data, ref Offset, 4);
                    Value = Enum.ToObject(ValueType, BitConverter.ToUInt32(Data, Offset));
                    Offset += 4;
                }
                else if (UnderlyingSystemType == typeof(long))
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    Value = Enum.ToObject(ValueType, BitConverter.ToInt64(Data, Offset));
                    Offset += 8;
                }
                else if (UnderlyingSystemType == typeof(ulong))
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    Value = Enum.ToObject(ValueType, BitConverter.ToUInt64(Data, Offset));
                    Offset += 8;
                }
                else
                {
                    ReadField(Input, ref Data, ref Offset, 4);
                    Value = Enum.ToObject(ValueType, BitConverter.ToInt32(Data, Offset));
                    Offset += 4;
                }
            }

            else
            {
                Value = null;
                return false;
            }

            return true;
        }

        private void ReadStringField(Stream Input, ref byte[] Data, ref int Offset, out string Value)
        {
            ReadField(Input, ref Data, ref Offset, CountByteSize);
            int CharCount = BitConverter.ToInt32(Data, Offset);
            if (CharCount > 10000)
                CharCount = 10000;

            Offset += CountByteSize;
            if (CharCount < 0)
                Value = null;
            else
            {
                ReadField(Input, ref Data, ref Offset, CharCount * 2);
                Value = Bytes2String(CharCount, Data, Offset);
                Offset += CharCount * 2;
            }
        }

        private void CreateObject(Type ReferenceType, ref object Reference)
        {
            try
            {
                Reference = Activator.CreateInstance(ReferenceType);
            }
            catch
            {
            }
        }

        private void CreateObject(Type ReferenceType, object[] Parameters, ref object Reference)
        {
            try
            {
                Reference = Activator.CreateInstance(ReferenceType, Parameters);
            }
            catch
            {
            }
        }

        private void CreateObject(Type ValueType, long Count, ref object Reference)
        {
            try
            {
                if (ValueType.IsArray)
                {
                    Type ArrayType = ValueType.GetElementType();
                    Reference = Array.CreateInstance(ArrayType, Count);
                }
                else if (Count < int.MaxValue)
                {
                    Reference = Activator.CreateInstance(ValueType, (int)Count);
                }
                else
                    Reference = Activator.CreateInstance(ValueType, Count);
            }
            catch
            {
            }
        }

        private Type DeserializedTrueType(string typeName)
        {
            return Type.GetType(typeName);
        }

        private void ProcessDeserializable(Stream Input, Type ReferenceType, ref byte[] Data, ref int Offset, out object Reference)
        {
            if (DeserializeBasicType(Input, ReferenceType, ref Data, ref Offset, out Reference))
                return;

            string ReferenceTypeName;
            ReadStringField(Input, ref Data, ref Offset, out ReferenceTypeName);

            if (ReferenceTypeName == null)
            {
                Reference = null;
                return;
            }

            ReferenceType = Type.GetType(ReferenceTypeName);
            Type OriginalType = ReferenceType;
            OverrideType(ref ReferenceType);
            Type NewType = ReferenceType;
            ReferenceType = OriginalType;

            if (ReferenceType.IsValueType)
            {
                CreateObject(NewType, ref Reference);
                Deserialize(Input, ref Reference, ReferenceType, -1, ref Data, ref Offset, null);
            }

            else
            {
                ReadField(Input, ref Data, ref Offset, 1);
                ObjectTag ReferenceTag = (ObjectTag)Data[Offset++];

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    int ReferenceIndex = (int)BitConverter.ToInt64(Data, Offset);
                    Offset += 8;

                    Reference = DeserializedObjectList[ReferenceIndex];
                }

                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    CreateObject(NewType, ref Reference);
                    AddDeserializedObject(Reference, ReferenceType, -1);
                }

                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    long Count = BitConverter.ToInt64(Data, Offset);
                    Offset += 8;

                    CreateObject(NewType, Count, ref Reference);
                    AddDeserializedObject(Reference, ReferenceType, Count);
                }

                else if (ReferenceTag == ObjectTag.ConstructedObject)
                {
                    List<SerializedMember> ConstructorParameters;
                    if (ListConstructorParameters(Reference, ReferenceType, out ConstructorParameters))
                    {
                        object[] Parameters = new object[ConstructorParameters.Count];

                        for (int i = 0; i < Parameters.Length; i++)
                        {
                            PropertyInfo AsPropertyInfo = ConstructorParameters[i].MemberInfo as PropertyInfo;

                            object MemberValue;
                            Type MemberType = AsPropertyInfo.PropertyType;
                            ProcessDeserializable(Input, MemberType, ref Data, ref Offset, out MemberValue);

                            Parameters[i] = MemberValue;
                        }

                        CreateObject(NewType, Parameters, ref Reference);
                        AddDeserializedObject(Reference, ReferenceType, -1);
                    }
                }
            }
        }

        private bool OverrideType(ref Type ReferenceType)
        {
            if (TypeOverrideTable.Count == 0 && AssemblyOverrideTable.Count == 0 && NamespaceOverrideTable.Count == 0)
                return false;

            if (TypeOverrideTable.Count > 0)
            {
                if (OverrideDirectType(ref ReferenceType))
                    return true;

                if (OverrideGenericDefinitionType(ref ReferenceType))
                    return true;
            }
            
            if (AssemblyOverrideTable.Count > 0 || NamespaceOverrideTable.Count > 0)
            {
                Type[] TypeList;
                if (ReferenceType.IsGenericType && !ReferenceType.IsGenericTypeDefinition)
                {
                    Type[] GenericArguments = ReferenceType.GetGenericArguments();
                    TypeList = new Type[1 + GenericArguments.Length];
                    TypeList[0] = ReferenceType.GetGenericTypeDefinition();
                    for (int i = 0; i < GenericArguments.Length; i++)
                        TypeList[i + 1] = GenericArguments[i];
                }
                else
                {
                    TypeList = new Type[1];
                    TypeList[0] = ReferenceType;
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
                        ReferenceType = TypeList[0];
                    else
                    {
                        Type[] GenericArguments = new Type[TypeList.Length - 1];
                        for (int i = 1; i < TypeList.Length; i++)
                            GenericArguments[i - 1] = TypeList[i];
                        ReferenceType = TypeList[0].MakeGenericType(GenericArguments);
                    }

                    return true;
                }
            }

            return false;
        }

        private bool OverrideDirectType(ref Type ReferenceType)
        {
            if (!TypeOverrideTable.ContainsKey(ReferenceType))
                return false;

            ReferenceType = TypeOverrideTable[ReferenceType];
            return true;
        }

        private bool OverrideGenericDefinitionType(ref Type ReferenceType)
        {
            if (!ReferenceType.IsGenericType || ReferenceType.IsGenericTypeDefinition)
                return false;

            bool Override = false;

            Type GenericTypeDefinition = ReferenceType.GetGenericTypeDefinition();
            Override |= OverrideType(ref GenericTypeDefinition);

            Type[] GenericArguments = ReferenceType.GetGenericArguments();
            if (OverrideGenericArguments)
                for (int i = 0; i < GenericArguments.Length; i++)
                    Override |= OverrideType(ref GenericArguments[i]);

            if (Override)
            {
                ReferenceType = GenericTypeDefinition.MakeGenericType(GenericArguments);
                return true;
            }

            return false;
        }

        private List<IDeserializedObject> DeserializedObjectList = new List<IDeserializedObject>();
        #endregion

        #region Check
        public bool Check(Stream Input)
        {
            return false;
        }
        #endregion

        #region Misc
        public static Type SerializableAncestor(Type ReferenceType)
        {
            Type t = ReferenceType;

            while (t != null && !t.Attributes.HasFlag(TypeAttributes.Serializable) && t.GetCustomAttribute(typeof(PolySerializer.SerializableAttribute)) == null)
                t = t.BaseType;

            return t;
        }

        private long GetCollectionCount(object Reference)
        {
            IEnumerable AsEnumerable;

            long Count = 0;
            if ((AsEnumerable = Reference as IEnumerable) != null)
            {
                IEnumerator Enumerator = AsEnumerable.GetEnumerator();

                Enumerator.Reset();
                while (Enumerator.MoveNext())
                    Count++;

                return Count;
            }
            else
                return -1;
        }

        private void AddSerializedObject(object Reference, long Count)
        {
            Type SerializedType = SerializableAncestor(Reference.GetType());
            SerializableObject NewSerialized = new SerializableObject(Reference, SerializedType, Count);
            SerializedObjectList.Add(NewSerialized);

            CycleDetectionTable.Add(Reference, NewSerialized);
        }

        private int SortByName(SerializedMember p1, SerializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private int SortByName(DeserializedMember p1, DeserializedMember p2)
        {
            return p1.MemberInfo.Name.CompareTo(p2.MemberInfo.Name);
        }

        private object GetPropertyValue(PropertyInfo SerializedProperty, object Reference)
        {
            return SerializedProperty.GetValue(Reference, null);
        }

        private void AddField(Stream Output, ref byte[] Data, ref int Offset, byte[] FieldContent)
        {
            if (Offset + FieldContent.Length > Data.Length)
            {
                Output.Write(Data, 0, Offset);
                Offset = 0;

                if (Data.Length < FieldContent.Length)
                    Data = new byte[FieldContent.Length];
            }

            for (int i = 0; i < FieldContent.Length; i++)
                Data[Offset++] = FieldContent[i];
        }

        private void ReadField(Stream Input, ref byte[] Data, ref int Offset, int MinLength)
        {
            bool Reload = false;

            if (Offset + MinLength > Data.Length)
            {
                int i;
                for (i = 0; i < Data.Length - Offset; i++)
                    Data[i] = Data[i + Offset];
                Offset = i;

                Reload = true;
            }
            else if (Offset == 0)
                Reload = true;

            if (Reload)
            {
                long Length = (Input.Length - Input.Position);
                if (Length > Data.Length - Offset)
                    Length = Data.Length - Offset;

                Input.Read(Data, Offset, (int)Length);
                Offset = 0;
            }
        }

        public static bool IsReadableCollection(Type t, object Reference, out IEnumerator Enumerator)
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
                        Enumerator = Interface.InvokeMember("GetEnumerator", BindingFlags.Public, null, Reference, null) as IEnumerator;
                        return true;
                    }

                CurrentType = CurrentType.BaseType;
            }

            Enumerator = null;
            return false;
        }

        public bool IsWriteableCollection(object Reference, Type ReferenceType, out IInserter Inserter, out Type ItemType)
        {
            foreach (IInserter TestInserter in CustomInserters)
            {
                Type TestType;
                if (TestInserter.TrySetReference(Reference, ReferenceType, out TestType))
                {
                    Inserter = TestInserter;
                    ItemType = TestType;
                    return true;
                }
            }

            foreach (IInserter TestInserter in BuiltInInserters)
            {
                Type TestType;
                if (TestInserter.TrySetReference(Reference, ReferenceType, out TestType))
                {
                    Inserter = TestInserter;
                    ItemType = TestType;
                    return true;
                }
            }

            Inserter = null;
            ItemType = null;
            return false;
        }

        private bool IsSerializableConstructor(ConstructorInfo Constructor, object Reference, Type SerializedType, out List<SerializedMember> ConstructorParameters)
        {
            SerializableAttribute CustomAttribute = Constructor.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomAttribute == null)
            {
                ConstructorParameters = null;
                return false;
            }

            if (CustomAttribute.Constructor == null)
            {
                ConstructorParameters = null;
                return false;
            }

            string[] Properties = CustomAttribute.Constructor.Split(',');
            ParameterInfo[] Parameters = Constructor.GetParameters();
            if (Properties.Length == 0 || Properties.Length != Parameters.Length)
            {
                ConstructorParameters = null;
                return false;
            }

            ConstructorParameters = new List<SerializedMember>();
            for (int i = 0; i < Properties.Length; i++)
            {
                string PropertyName = Properties[i].Trim();
                MemberInfo[] Members = SerializedType.GetMember(PropertyName);
                if (Members.Length != 1)
                    return false;

                MemberInfo Member = Members[0];
                if (Member.MemberType != MemberTypes.Property)
                    return false;

                SerializedMember NewMember = new SerializedMember(Member);
                ConstructorParameters.Add(NewMember);
            }

            return true;
        }

        private bool IsSerializableMember(object Reference, Type SerializedType, SerializedMember NewMember)
        {
            if (NewMember.MemberInfo.MemberType != MemberTypes.Field && NewMember.MemberInfo.MemberType != MemberTypes.Property)
                return false;

            if (IsStaticOrReadOnly(NewMember.MemberInfo))
                return false;

            if (IsExcludedFromSerialization(NewMember))
                return false;

            if (IsReadOnlyPropertyWithNoSetter(NewMember))
                return false;

            if (IsExcludedIndexer(NewMember))
                return false;

            CheckSerializationCondition(Reference, SerializedType, NewMember);

            return true;
        }

        private bool IsStaticOrReadOnly(MemberInfo MemberInfo)
        {
            FieldInfo AsFieldInfo;
            if ((AsFieldInfo = MemberInfo as FieldInfo) != null)
            {
                if (AsFieldInfo.Attributes.HasFlag(FieldAttributes.Static) || AsFieldInfo.Attributes.HasFlag(FieldAttributes.InitOnly))
                    return true;
            }

            return false;
        }

        private bool IsExcludedFromSerialization(SerializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private bool IsReadOnlyPropertyWithNoSetter(SerializedMember NewMember)
        {
            PropertyInfo AsPropertyInfo;
            if ((AsPropertyInfo = NewMember.MemberInfo as PropertyInfo) != null)
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

            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Setter != null)
                    return false;
            }

            return true;
        }

        private bool IsExcludedIndexer(SerializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
                return false;

            if (NewMember.MemberInfo.Name == "Item" && NewMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private void CheckSerializationCondition(object Reference, Type SerializedType, SerializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Condition != null)
                {
                    MemberInfo[] ConditionMembers = SerializedType.GetMember(CustomSerializable.Condition);
                    if (ConditionMembers != null)
                    {
                        foreach (MemberInfo ConditionMember in ConditionMembers)
                        {
                            FieldInfo AsFieldInfo;
                            PropertyInfo AsPropertyInfo;

                            if ((AsFieldInfo = ConditionMember as FieldInfo) != null)
                            {
                                if (AsFieldInfo.FieldType == typeof(bool))
                                    NewMember.SetCondition((bool)AsFieldInfo.GetValue(Reference));
                            }

                            else if ((AsPropertyInfo = ConditionMember as PropertyInfo) != null)
                            {
                                if (AsPropertyInfo.PropertyType == typeof(bool))
                                    NewMember.SetCondition((bool)AsPropertyInfo.GetValue(Reference));
                            }
                        }
                    }
                }
            }
        }

        private void AddDeserializedObject(object Reference, Type DeserializedType, long Count)
        {
            DeserializedObjectList.Add(new DeserializedObject(Reference, DeserializedType, Count));
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

        private string Bytes2String(int CharCount, byte[] Data, int Offset)
        {
            int i = Offset;
            char[] StringChars = new char[CharCount];

            for (i = 0; i < CharCount; i++)
                StringChars[i] = BitConverter.ToChar(Data, Offset + i * 2);

            return new string(StringChars);
        }

        private bool ListConstructorParameters(object Reference, Type SerializedType, out List<SerializedMember> ConstructorParameters)
        {
            List<ConstructorInfo> Constructors = new List<ConstructorInfo>(SerializedType.GetConstructors());

            foreach (ConstructorInfo Constructor in Constructors)
                if (IsSerializableConstructor(Constructor, Reference, SerializedType, out ConstructorParameters))
                    return true;

            ConstructorParameters = null;
            return false;
        }

        private List<SerializedMember> ListSerializedMembers(object Reference, Type SerializedType, ref byte[] Data, ref int Offset)
        {
            List<MemberInfo> Members = new List<MemberInfo>(SerializedType.GetMembers());
            List<SerializedMember> SerializedMembers = new List<SerializedMember>();

            foreach (MemberInfo MemberInfo in Members)
            {
                SerializedMember NewMember = new SerializedMember(MemberInfo);

                if (IsSerializableMember(Reference, SerializedType, NewMember))
                    SerializedMembers.Add(NewMember);
            }

            if (Mode == SerializationMode.Default)
                SerializedMembers.Sort(SortByName);

            else if (Mode == SerializationMode.MemberName)
            {
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(SerializedMembers.Count));

                foreach (SerializedMember Member in SerializedMembers)
                    AddField(Output, ref Data, ref Offset, String2Bytes(Member.MemberInfo.Name));
            }

            return SerializedMembers;
        }

        private List<DeserializedMember> ListDeserializedMembers(Stream Input, Type DeserializedType, ref byte[] Data, ref int Offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                ReadField(Input, ref Data, ref Offset, 4);
                int MemberCount = BitConverter.ToInt32(Data, Offset);
                Offset += 4;

                for (int i = 0; i < MemberCount; i++)
                {
                    string MemberName;
                    ReadStringField(Input, ref Data, ref Offset, out MemberName);

                    MemberInfo[] MatchingMembers = DeserializedType.GetMember(MemberName);
                    DeserializedMember NewMember = new DeserializedMember(MatchingMembers[0]);

                    CheckForSerializedCondition(NewMember);
                    DeserializedMembers.Add(NewMember);
                }
            }
            else
            {
                List<MemberInfo> Members = new List<MemberInfo>(DeserializedType.GetMembers());

                foreach (MemberInfo MemberInfo in Members)
                {
                    DeserializedMember NewMember = new DeserializedMember(MemberInfo);

                    if (!IsDeserializableMember(DeserializedType, NewMember))
                        continue;

                    DeserializedMembers.Add(NewMember);
                }

                if (Mode == SerializationMode.Default)
                    DeserializedMembers.Sort(SortByName);
            }

            return DeserializedMembers;
        }

        private bool IsDeserializableMember(Type DeserializedType, DeserializedMember NewMember)
        {
            if (NewMember.MemberInfo.MemberType != MemberTypes.Field && NewMember.MemberInfo.MemberType != MemberTypes.Property)
                return false;

            if (IsStaticOrReadOnly(NewMember.MemberInfo))
                return false;

            if (IsExcludedFromDeserialization(NewMember))
                return false;

            if (IsReadOnlyPropertyWithNoValidSetter(DeserializedType, NewMember))
                return false;

            if (IsExcludedIndexer(NewMember))
                return false;

            CheckForSerializedCondition(NewMember);

            return true;
        }

        private bool IsExcludedFromDeserialization(DeserializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Exclude)
                    return true;
            }

            return false;
        }

        private bool IsReadOnlyPropertyWithNoValidSetter(Type DeserializedType, DeserializedMember NewMember)
        {
            PropertyInfo AsPropertyInfo;
            if ((AsPropertyInfo = NewMember.MemberInfo as PropertyInfo) != null)
            {
                if (AsPropertyInfo.CanWrite)
                {
                    Debug.Assert(AsPropertyInfo.SetMethod != null);
                    MethodInfo Setter = AsPropertyInfo.SetMethod;
                    if (Setter.Attributes.HasFlag(MethodAttributes.Public))
                        return false;
                }

                SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
                if (CustomSerializable != null)
                {
                    if (CustomSerializable.Setter != null)
                    {
                        MemberInfo[] SetterMembers = DeserializedType.GetMember(CustomSerializable.Setter);
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
                                            NewMember.SetPropertySetter(AsMethodInfo);
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

        private bool IsExcludedIndexer(DeserializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
                return false;

            if (NewMember.MemberInfo.Name == "Item" && NewMember.MemberInfo.MemberType == MemberTypes.Property)
                return true;

            return false;
        }

        private void CheckForSerializedCondition(DeserializedMember NewMember)
        {
            SerializableAttribute CustomSerializable = NewMember.MemberInfo.GetCustomAttribute(typeof(SerializableAttribute)) as SerializableAttribute;
            if (CustomSerializable != null)
            {
                if (CustomSerializable.Condition != null)
                    NewMember.SetHasCondition();
            }
        }
        #endregion
    }
}
