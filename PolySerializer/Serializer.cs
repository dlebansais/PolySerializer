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
        public SerializationFormat FileFormat { get; set; } = SerializationFormat.BinaryPreferred;
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

            IsSerializedAsText = (FileFormat == SerializationFormat.TextPreferred) || (FileFormat == SerializationFormat.TextOnly);

            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"Mode={Mode}\n");
            else
                AddFieldInt(Output, ref Data, ref Offset, (int)Mode);

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
                    AddFieldBool(Output, ref Data, ref Offset, Member.Condition.Value);
                    if (!Member.Condition.Value)
                        continue;

                    if (IsSerializedAsText)
                        AddFieldStringDirect(Output, ref Data, ref Offset, " ");
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
                AddFieldSByte(Output, ref Data, ref Offset, (sbyte)Value);

            else if (ValueType == typeof(byte))
                AddFieldByte(Output, ref Data, ref Offset, (byte)Value);

            else if (ValueType == typeof(bool))
                AddFieldBool(Output, ref Data, ref Offset, (bool)Value);

            else if (ValueType == typeof(char))
                AddFieldChar(Output, ref Data, ref Offset, (char)Value);

            else if (ValueType == typeof(decimal))
                AddFieldDecimal(Output, ref Data, ref Offset, (decimal)Value);

            else if (ValueType == typeof(double))
                AddFieldDouble(Output, ref Data, ref Offset, (double)Value);

            else if (ValueType == typeof(float))
                AddFieldFloat(Output, ref Data, ref Offset, (float)Value);

            else if (ValueType == typeof(int))
                AddFieldInt(Output, ref Data, ref Offset, (int)Value);

            else if (ValueType == typeof(long))
                AddFieldLong(Output, ref Data, ref Offset, (long)Value);

            else if (ValueType == typeof(short))
                AddFieldShort(Output, ref Data, ref Offset, (short)Value);

            else if (ValueType == typeof(uint))
                AddFieldUInt(Output, ref Data, ref Offset, (uint)Value);

            else if (ValueType == typeof(ulong))
                AddFieldULong(Output, ref Data, ref Offset, (ulong)Value);

            else if (ValueType == typeof(ushort))
                AddFieldUShort(Output, ref Data, ref Offset, (ushort)Value);

            else if (ValueType == typeof(string))
                AddFieldString(Output, ref Data, ref Offset, (string)Value);

            else if (ValueType == typeof(Guid))
                AddFieldGuid(Output, ref Data, ref Offset, (Guid)Value);

            else if (ValueType.IsEnum)
            {
                Type UnderlyingSystemType = ValueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                    AddFieldSByte(Output, ref Data, ref Offset, (sbyte)Value);
                else if (UnderlyingSystemType == typeof(byte))
                    AddFieldByte(Output, ref Data, ref Offset, (byte)Value);
                else if (UnderlyingSystemType == typeof(short))
                    AddFieldShort(Output, ref Data, ref Offset, (short)Value);
                else if (UnderlyingSystemType == typeof(ushort))
                    AddFieldUShort(Output, ref Data, ref Offset, (ushort)Value);
                else if (UnderlyingSystemType == typeof(int))
                    AddFieldInt(Output, ref Data, ref Offset, (int)Value);
                else if (UnderlyingSystemType == typeof(uint))
                    AddFieldUInt(Output, ref Data, ref Offset, (uint)Value);
                else if (UnderlyingSystemType == typeof(long))
                    AddFieldLong(Output, ref Data, ref Offset, (long)Value);
                else if (UnderlyingSystemType == typeof(ulong))
                    AddFieldULong(Output, ref Data, ref Offset, (ulong)Value);
                else
                    AddFieldInt(Output, ref Data, ref Offset, (int)Value);
            }

            else
                return false;

            return true;
        }

        private void ProcessSerializable(object Reference, ref byte[] Data, ref int Offset)
        {
            if (Reference == null)
            {
                AddFieldNull(Output, ref Data, ref Offset);
                return;
            }

            if (SerializeBasicType(Reference, ref Data, ref Offset))
                return;

            Type ReferenceType = SerializableAncestor(Reference.GetType());
            AddFieldType(Output, ref Data, ref Offset, ReferenceType);

            if (ReferenceType.IsValueType)
                Serialize(Output, Reference, ReferenceType, -1, ref Data, ref Offset, null);
            else
            {
                if (CycleDetectionTable.ContainsKey(Reference))
                {
                    long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[Reference]);

                    if (IsSerializedAsText)
                        AddFieldStringDirect(Output, ref Data, ref Offset, $" #{ReferenceIndex}\n");
                    else
                    {
                        AddFieldByte(Output, ref Data, ref Offset, (byte)ObjectTag.ObjectIndex);
                        AddFieldLong(Output, ref Data, ref Offset, ReferenceIndex);
                    }
                }
                else
                {
                    long Count = GetCollectionCount(Reference);
                    if (Count < 0)
                    {
                        List<SerializedMember> ConstructorParameters;
                        if (ListConstructorParameters(Reference, ReferenceType, out ConstructorParameters))
                        {
                            if (IsSerializedAsText)
                                AddFieldStringDirect(Output, ref Data, ref Offset, " ()\n");
                            else
                                AddFieldByte(Output, ref Data, ref Offset, (byte)ObjectTag.ConstructedObject);

                            foreach (SerializedMember Member in ConstructorParameters)
                            {
                                PropertyInfo AsPropertyInfo;
                                AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                                object MemberValue = AsPropertyInfo.GetValue(Reference);

                                ProcessSerializable(MemberValue, ref Data, ref Offset);
                            }
                        }
                        else
                        {
                            if (IsSerializedAsText)
                                AddFieldStringDirect(Output, ref Data, ref Offset, "\n");
                            else
                                AddFieldByte(Output, ref Data, ref Offset, (byte)ObjectTag.ObjectReference);
                        }
                    }
                    else
                    {
                        if (IsSerializedAsText)
                            AddFieldStringDirect(Output, ref Data, ref Offset, $" *{Count}\n");
                        else
                        {
                            AddFieldByte(Output, ref Data, ref Offset, (byte)ObjectTag.ObjectList);
                            AddFieldLong(Output, ref Data, ref Offset, Count);
                        }
                    }

                    AddSerializedObject(Reference, Count);
                }
            }
        }

        private List<ISerializableObject> SerializedObjectList = new List<ISerializableObject>();
        private bool IsSerializedAsText;
        #endregion

        #region Deserialization
        public object Deserialize(Stream Input)
        {
            byte[] Data = new byte[MinAllocatedSize];
            int Offset = 0;

            ReadField(Input, ref Data, ref Offset, 4);

            if (FileFormat == SerializationFormat.TextPreferred || FileFormat == SerializationFormat.BinaryPreferred)
                IsSerializedAsText = (Data[0] == 'M' && Data[1] == 'o' && Data[2] == 'd' && Data[3] == 'e');
            else
                IsSerializedAsText = (FileFormat == SerializationFormat.TextOnly);

            if (IsSerializedAsText)
            {
                Offset += 4;

                ReadField(Input, ref Data, ref Offset, 8);
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
                    bool ConditionValue = ReadFieldBool(Input, ref Data, ref Offset);
                    if (!ConditionValue)
                        continue;

                    if (IsSerializedAsText)
                    {
                        ReadField(Input, ref Data, ref Offset, 1);
                        Offset++;
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
                Value = ReadFieldSByte(Input, ref Data, ref Offset);

            else if (ValueType == typeof(byte))
                Value = ReadFieldByte(Input, ref Data, ref Offset);

            else if (ValueType == typeof(bool))
                Value = ReadFieldBool(Input, ref Data, ref Offset);

            else if (ValueType == typeof(char))
                Value = ReadFieldChar(Input, ref Data, ref Offset);

            else if (ValueType == typeof(decimal))
                Value = ReadFieldDecimal(Input, ref Data, ref Offset);

            else if (ValueType == typeof(double))
                Value = ReadFieldDouble(Input, ref Data, ref Offset);

            else if (ValueType == typeof(float))
                Value = ReadFieldFloat(Input, ref Data, ref Offset);

            else if (ValueType == typeof(int))
                Value = ReadFieldInt(Input, ref Data, ref Offset);

            else if (ValueType == typeof(long))
                Value = ReadFieldLong(Input, ref Data, ref Offset);

            else if (ValueType == typeof(short))
                Value = ReadFieldShort(Input, ref Data, ref Offset);

            else if (ValueType == typeof(uint))
                Value = ReadFieldUInt(Input, ref Data, ref Offset);

            else if (ValueType == typeof(ulong))
                Value = ReadFieldULong(Input, ref Data, ref Offset);

            else if (ValueType == typeof(ushort))
                Value = ReadFieldUShort(Input, ref Data, ref Offset);

            else if (ValueType == typeof(string))
                Value = ReadFieldString(Input, ref Data, ref Offset);
            
            else if (ValueType == typeof(Guid))
                Value = ReadFieldGuid(Input, ref Data, ref Offset);

            else if (ValueType != null && ValueType.IsEnum)
            {
                Type UnderlyingSystemType = ValueType.GetEnumUnderlyingType();
                if (UnderlyingSystemType == typeof(sbyte))
                    Value = Enum.ToObject(ValueType, ReadFieldSByte(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(byte))
                    Value = Enum.ToObject(ValueType, ReadFieldByte(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(short))
                    Value = Enum.ToObject(ValueType, ReadFieldShort(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(ushort))
                    Value = Enum.ToObject(ValueType, ReadFieldUShort(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(int))
                    Value = Enum.ToObject(ValueType, ReadFieldInt(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(uint))
                    Value = Enum.ToObject(ValueType, ReadFieldUInt(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(long))
                    Value = Enum.ToObject(ValueType, ReadFieldLong(Input, ref Data, ref Offset));
                else if (UnderlyingSystemType == typeof(ulong))
                    Value = Enum.ToObject(ValueType, ReadFieldULong(Input, ref Data, ref Offset));
                else
                    Value = Enum.ToObject(ValueType, ReadFieldInt(Input, ref Data, ref Offset));
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
            //if (CharCount > 10000)
            //    CharCount = 10000;

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

            string ReferenceTypeName = ReadFieldType(Input, ref Data, ref Offset);
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
                ObjectTag ReferenceTag = ReadFieldTag(Input, ref Data, ref Offset);

                if (ReferenceTag == ObjectTag.ObjectIndex)
                {
                    int ReferenceIndex = ReadFieldObjectIndex(Input, ref Data, ref Offset);
                    Reference = DeserializedObjectList[ReferenceIndex].Reference;
                }

                else if (ReferenceTag == ObjectTag.ObjectReference)
                {
                    CreateObject(NewType, ref Reference);
                    AddDeserializedObject(Reference, ReferenceType, -1);
                }

                else if (ReferenceTag == ObjectTag.ObjectList)
                {
                    long Count = ReadFieldCount(Input, ref Data, ref Offset);

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

        private void AddFieldSByte(Stream Output, ref byte[] Data, ref int Offset, sbyte value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{((byte)value).ToString("X02")}");
            else
                AddField(Output, ref Data, ref Offset, new byte[1] { (byte)value });
        }

        private void AddFieldByte(Stream Output, ref byte[] Data, ref int Offset, byte value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X02")}");
            else
                AddField(Output, ref Data, ref Offset, new byte[1] { value });
        }

        private void AddFieldBool(Stream Output, ref byte[] Data, ref int Offset, bool value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"{value}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldChar(Stream Output, ref byte[] Data, ref int Offset, char value)
        {
            if (IsSerializedAsText)
            {
                if (value == '\'')
                    AddFieldStringDirect(Output, ref Data, ref Offset, @"'\''");
                else
                    AddFieldStringDirect(Output, ref Data, ref Offset, $"'{value}'");
            }
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldDecimal(Stream Output, ref byte[] Data, ref int Offset, decimal value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"{value.ToString(CultureInfo.InvariantCulture)}m");
            else
            {
                int[] DecimalInts = decimal.GetBits(value);
                for (int i = 0; i < 4; i++)
                {
                    byte[] DecimalBytes = BitConverter.GetBytes(DecimalInts[i]);
                    AddField(Output, ref Data, ref Offset, DecimalBytes);
                }
            }
        }

        private void AddFieldDouble(Stream Output, ref byte[] Data, ref int Offset, double value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"{value.ToString(CultureInfo.InvariantCulture)}d");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldFloat(Stream Output, ref byte[] Data, ref int Offset, float value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"{value.ToString(CultureInfo.InvariantCulture)}f");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldInt(Stream Output, ref byte[] Data, ref int Offset, int value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X08")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldLong(Stream Output, ref byte[] Data, ref int Offset, long value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X16")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldShort(Stream Output, ref byte[] Data, ref int Offset, short value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X04")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUInt(Stream Output, ref byte[] Data, ref int Offset, uint value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X08")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldULong(Stream Output, ref byte[] Data, ref int Offset, ulong value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X16")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldUShort(Stream Output, ref byte[] Data, ref int Offset, ushort value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"0x{value.ToString("X04")}");
            else
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(value));
        }

        private void AddFieldString(Stream Output, ref byte[] Data, ref int Offset, string value)
        {
            if (IsSerializedAsText)
            {
                if (value == null)
                    value = "null";
                else
                    value = "\"" + value.Replace("\"", "\\\"") + "\"";

                AddField(Output, ref Data, ref Offset, Encoding.UTF8.GetBytes(value));
            }
            else
                AddField(Output, ref Data, ref Offset, String2Bytes(value));
        }

        private void AddFieldGuid(Stream Output, ref byte[] Data, ref int Offset, Guid value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, value.ToString("B"));
            else
                AddField(Output, ref Data, ref Offset, value.ToByteArray());
        }

        private void AddFieldNull(Stream Output, ref byte[] Data, ref int Offset)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, "null");
            else
                AddField(Output, ref Data, ref Offset, new byte[CountByteSize] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        }

        private void AddFieldType(Stream Output, ref byte[] Data, ref int Offset, Type value)
        {
            if (IsSerializedAsText)
                AddFieldStringDirect(Output, ref Data, ref Offset, $"{{{value.AssemblyQualifiedName}}}");
            else
                AddFieldString(Output, ref Data, ref Offset, value.AssemblyQualifiedName);
        }

        private void AddFieldStringDirect(Stream Output, ref byte[] Data, ref int Offset, string s)
        {
            AddField(Output, ref Data, ref Offset, Encoding.UTF8.GetBytes(s));
        }

        private void AddFieldMembers(Stream Output, ref byte[] Data, ref int Offset, List<SerializedMember> SerializedMembers)
        {
            if (IsSerializedAsText)
            {
                for (int i = 0; i < SerializedMembers.Count; i++)
                {
                    if (i > 0)
                        AddFieldStringDirect(Output, ref Data, ref Offset, ",");

                    SerializedMember Member = SerializedMembers[i];
                    AddFieldStringDirect(Output, ref Data, ref Offset, Member.MemberInfo.Name);
                }

                AddFieldStringDirect(Output, ref Data, ref Offset, "\n");
            }
            else
            {
                AddFieldInt(Output, ref Data, ref Offset, SerializedMembers.Count);

                foreach (SerializedMember Member in SerializedMembers)
                    AddFieldString(Output, ref Data, ref Offset, Member.MemberInfo.Name);
            }
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

        private sbyte ReadFieldSByte(Stream Input, ref byte[] Data, ref int Offset)
        {
            sbyte Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 4);

                uint n = 0;
                for (int i = 0; i < 2; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = (sbyte)n;
                Offset += 4;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = (sbyte)Data[Offset];
                Offset++;
            }

            return Value;
        }

        private byte ReadFieldByte(Stream Input, ref byte[] Data, ref int Offset)
        {
            byte Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 4);

                uint n = 0;
                for (int i = 0; i < 2; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = (byte)n;
                Offset += 4;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = Data[Offset];
                Offset++;
            }

            return Value;
        }

        private bool ReadFieldBool(Stream Input, ref byte[] Data, ref int Offset)
        {
            bool Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 4);

                Value = (Data[Offset + 0] == 'T' && Data[Offset + 1] == 'r' && Data[Offset + 2] == 'u' && Data[Offset + 3] == 'e');
                Offset += 4;

                if (!Value)
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    Offset++;
                }
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 1);
                Value = BitConverter.ToBoolean(Data, Offset);
                Offset += 1;
            }

            return Value;
        }

        private char ReadFieldChar(Stream Input, ref byte[] Data, ref int Offset)
        {
            char Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 2);
                int CharOffset = Offset;
                Offset += 2;

                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != '\'');

                if (Offset == CharOffset + 4 && Data[CharOffset + 1] == '\\' && Data[CharOffset + 2] == '\'')
                    Value = '\'';
                else
                    Value = Encoding.UTF8.GetString(Data, CharOffset + 1, Offset - CharOffset - 2)[0];
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToChar(Data, Offset);
                Offset += 2;
            }

            return Value;
        }

        private decimal ReadFieldDecimal(Stream Input, ref byte[] Data, ref int Offset)
        {
            decimal Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;
                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != 'm');

                string s = Encoding.UTF8.GetString(Data, BaseOffset, Offset - BaseOffset - 1);
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Parsed))
                    Value = Parsed;
                else
                    Value = default(decimal);
            }
            else
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

            return Value;
        }

        private double ReadFieldDouble(Stream Input, ref byte[] Data, ref int Offset)
        {
            double Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;
                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != 'd');

                string s = Encoding.UTF8.GetString(Data, BaseOffset, Offset - BaseOffset - 1);
                if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out double Parsed))
                    Value = Parsed;
                else
                    Value = default(double);
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToDouble(Data, Offset);
                Offset += 8;
            }

            return Value;
        }

        private float ReadFieldFloat(Stream Input, ref byte[] Data, ref int Offset)
        {
            float Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;
                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != 'f');

                string s = Encoding.UTF8.GetString(Data, BaseOffset, Offset - BaseOffset - 1);
                if (float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out float Parsed))
                    Value = Parsed;
                else
                    Value = default(float);
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToSingle(Data, Offset);
                Offset += 4;
            }

            return Value;
        }

        private int ReadFieldInt(Stream Input, ref byte[] Data, ref int Offset)
        {
            int Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 10);

                uint n = 0;
                for (int i = 0; i < 8; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = (int)n;
                Offset += 10;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToInt32(Data, Offset);
                Offset += 4;
            }

            return Value;
        }

        private long ReadFieldLong(Stream Input, ref byte[] Data, ref int Offset)
        {
            long Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 18);

                ulong n = 0;
                for (int i = 0; i < 16; i++)
                    n = (n * 16) + (ulong)FromHexDigit(Data, Offset + 2 + i);

                Value = (long)n;
                Offset += 18;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToInt64(Data, Offset);
                Offset += 8;
            }

            return Value;
        }

        private short ReadFieldShort(Stream Input, ref byte[] Data, ref int Offset)
        {
            short Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 6);

                uint n = 0;
                for (int i = 0; i < 4; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = (short)n;
                Offset += 6;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToInt16(Data, Offset);
                Offset += 2;
            }

            return Value;
        }

        private uint ReadFieldUInt(Stream Input, ref byte[] Data, ref int Offset)
        {
            uint Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 10);

                uint n = 0;
                for (int i = 0; i < 8; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = n;
                Offset += 10;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToUInt32(Data, Offset);
                Offset += 4;
            }

            return Value;
        }

        private ulong ReadFieldULong(Stream Input, ref byte[] Data, ref int Offset)
        {
            ulong Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 18);

                ulong n = 0;
                for (int i = 0; i < 16; i++)
                    n = (n * 16) + (ulong)FromHexDigit(Data, Offset + 2 + i);

                Value = n;
                Offset += 18;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 8);
                Value = BitConverter.ToUInt64(Data, Offset);
                Offset += 8;
            }

            return Value;
        }

        private ushort ReadFieldUShort(Stream Input, ref byte[] Data, ref int Offset)
        {
            ushort Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 6);

                uint n = 0;
                for (int i = 0; i < 4; i++)
                    n = (n * 16) + (uint)FromHexDigit(Data, Offset + 2 + i);

                Value = (ushort)n;
                Offset += 6;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 2);
                Value = BitConverter.ToUInt16(Data, Offset);
                Offset += 2;
            }

            return Value;
        }

        private string ReadFieldString(Stream Input, ref byte[] Data, ref int Offset)
        {
            string Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 1);
                if (Data[Offset] == 'n')
                {
                    Offset++;
                    ReadField(Input, ref Data, ref Offset, 3);
                    if (Data[Offset + 0] == 'u' && Data[Offset + 1] == 'l' && Data[Offset + 2] == 'l')
                        Offset += 3;

                    return null;
                }

                if (Data[Offset] != '"')
                {
                    Offset++;
                    return null;
                }

                int BaseOffset = Offset++;

                for (;;)
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    if (Data[Offset] == '\\')
                    {
                        Offset++;
                        ReadField(Input, ref Data, ref Offset, 1);
                    }
                    else if (Data[Offset] == '"')
                    {
                        Offset++;
                        break;
                    }

                    Offset++;
                }

                string Content = Encoding.UTF8.GetString(Data, BaseOffset + 1, Offset - BaseOffset - 2);
                Value = Content.Replace("\\\"", "\"");
            }
            else
            {
                string StringValue;
                ReadStringField(Input, ref Data, ref Offset, out StringValue);
                Value = StringValue;
            }

            return Value;
        }

        private Guid ReadFieldGuid(Stream Input, ref byte[] Data, ref int Offset)
        {
            Guid Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 38);
                string Content = Encoding.UTF8.GetString(Data, Offset, 38);
                Offset += 38;

                if (Guid.TryParse(Content, out Guid AsGuid))
                    Value = AsGuid;
                else
                    Value = Guid.Empty;
            }
            else
            {
                ReadField(Input, ref Data, ref Offset, 16);
                byte[] GuidBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                    GuidBytes[i] = Data[Offset++];
                Value = new Guid(GuidBytes);
                //Value = Guid.NewGuid();
            }

            return Value;
        }

        private string ReadFieldType(Stream Input, ref byte[] Data, ref int Offset)
        {
            string Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;

                ReadField(Input, ref Data, ref Offset, 1);
                if (Data[Offset] != '{')
                {
                    if (Data[Offset++] == 'n')
                    {
                        ReadField(Input, ref Data, ref Offset, 3);
                        if (Data[Offset + 0] == 'u' && Data[Offset + 1] == 'l' && Data[Offset + 2] == 'l')
                            Offset += 3;
                    }

                    return null;
                }

                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != '}');

                Value = Encoding.UTF8.GetString(Data, BaseOffset + 1, Offset - BaseOffset - 2);
            }
            else
            {
                ReadStringField(Input, ref Data, ref Offset, out string AsString);
                Value = AsString;
            }

            return Value;
        }

        private void ReadFieldMembers(Stream Input, ref byte[] Data, ref int Offset, out List<string> MemberNames)
        {
            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;

                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != '\n');

                string AllNames = Encoding.UTF8.GetString(Data, BaseOffset, Offset - BaseOffset - 1);
                string[] Splitted = AllNames.Split(',');
                MemberNames = new List<string>(Splitted);
            }
            else
            {
                MemberNames = new List<string>();

                ReadField(Input, ref Data, ref Offset, 4);
                int MemberCount = BitConverter.ToInt32(Data, Offset);
                Offset += 4;

                for (int i = 0; i < MemberCount; i++)
                {
                    string MemberName;
                    ReadStringField(Input, ref Data, ref Offset, out MemberName);
                    MemberNames.Add(MemberName);
                }
            }
        }

        private ObjectTag ReadFieldTag(Stream Input, ref byte[] Data, ref int Offset)
        {
            ObjectTag Value;

            if (IsSerializedAsText)
            {
                ReadField(Input, ref Data, ref Offset, 1);
                char c = (char)Data[Offset++];
                if (c == '\n')
                    Value = ObjectTag.ObjectReference;
                else if (c == ' ')
                {
                    ReadField(Input, ref Data, ref Offset, 1);
                    c = (char)Data[Offset++];

                    if (c == '#')
                        Value = ObjectTag.ObjectIndex;
                    else if (c == '(')
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
                ReadField(Input, ref Data, ref Offset, 1);
                Value = (ObjectTag)Data[Offset++];
            }

            return Value;
        }

        private int ReadFieldObjectIndex(Stream Input, ref byte[] Data, ref int Offset)
        {
            int Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;
                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != '\n');

                int n = 0;
                for (int i = BaseOffset; i + 1 < Offset; i++)
                    n = (n * 10) + FromDecimalDigit(Data, i);

                Value = n;
            }
            else
                Value = (int)ReadFieldLong(Input, ref Data, ref Offset);

            return Value;
        }

        private long ReadFieldCount(Stream Input, ref byte[] Data, ref int Offset)
        {
            long Value;

            if (IsSerializedAsText)
            {
                int BaseOffset = Offset;
                do
                    ReadField(Input, ref Data, ref Offset, 1);
                while (Data[Offset++] != '\n');

                long n = 0;
                for (int i = BaseOffset; i + 1 < Offset; i++)
                    n = (n * 10) + FromDecimalDigit(Data, i);

                Value = n;
            }
            else
                Value = ReadFieldLong(Input, ref Data, ref Offset);

            return Value;
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

        private int FromHexDigit(byte[] Data, int Offset)
        {
            int Digit = Data[Offset];
            if (Digit >= '0' && Digit <= '9')
                return Digit - '0';
            else if (Digit >= 'a' && Digit <= 'f')
                return Digit - 'a' + 10;
            else if (Digit >= 'A' && Digit <= 'F')
                return Digit - 'A' + 10;
            else
                return 0;
        }

        private int FromDecimalDigit(byte[] Data, int Offset)
        {
            int Digit = Data[Offset];
            if (Digit >= '0' && Digit <= '9')
                return Digit - '0';
            else
                return 0;
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
                AddFieldMembers(Output, ref Data, ref Offset, SerializedMembers);

            return SerializedMembers;
        }

        private List<DeserializedMember> ListDeserializedMembers(Stream Input, Type DeserializedType, ref byte[] Data, ref int Offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                List<string> MemberNames;
                ReadFieldMembers(Input, ref Data, ref Offset, out MemberNames);

                foreach (string MemberName in MemberNames)
                {
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
