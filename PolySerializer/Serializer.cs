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
        /// Gets or sets a list of types that can override the original type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; }

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
        public IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; } = new Dictionary<Type, Type>();
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

            string RootTypeName = Root.GetType().AssemblyQualifiedName;
            AddField(Output, ref Data, ref Offset, String2Bytes(RootTypeName));
            AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Mode));

            SerializedObjectList.Clear();
            CycleDetectionTable.Clear();
            ProcessSerializable(Root, RootType, ref Data, ref Offset);

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
                    Type ItemType = Item.GetType();
                    ProcessSerializable(Item, ItemType, ref Data, ref Offset);
                }
            }

            List<SerializedMember> SerializedMembers = ListSerializedMembers(Reference, SerializedType, ref Data, ref Offset);

            foreach (SerializedMember Member in SerializedMembers)
            {
                if (Member.Condition.HasValue)
                {
                    SerializeBasicType(Member.Condition.Value, typeof(bool), ref Data, ref Offset);
                    if (!Member.Condition.Value)
                        continue;
                }

                object MemberValue;
                Type MemberType;

                FieldInfo AsFieldInfo;
                PropertyInfo AsPropertyInfo;

                if ((AsFieldInfo = Member.MemberInfo as FieldInfo) != null)
                {
                    MemberValue = AsFieldInfo.GetValue(Reference);
                    MemberType = AsFieldInfo.FieldType;
                }

                else
                {
                    AsPropertyInfo = Member.MemberInfo as PropertyInfo;
                    MemberValue = AsPropertyInfo.GetValue(Reference);
                    MemberType = AsPropertyInfo.PropertyType;
                }

                ProcessSerializable(MemberValue, MemberType, ref Data, ref Offset);
            }

            if (NextSerialized != null)
                NextSerialized.SetSerialized();
        }

        private bool SerializeBasicType(object Value, Type ValueType, ref byte[] Data, ref int Offset)
        {
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

            else if (ValueType.IsEnum)
                AddField(Output, ref Data, ref Offset, BitConverter.GetBytes((int)Value));

            else
                return false;

            return true;
        }

        private void ProcessSerializable(object Reference, Type ReferenceType, ref byte[] Data, ref int Offset)
        {
            if (!SerializeBasicType(Reference, ReferenceType, ref Data, ref Offset))
            {
                if (ReferenceType.IsValueType)
                    Serialize(Output, Reference, ReferenceType, -1, ref Data, ref Offset, null);

                else if (Reference == null)
                    AddField(Output, ref Data, ref Offset, new byte[1] { 0 });

                else
                {
                    if (CycleDetectionTable.ContainsKey(Reference))
                    {
                        AddField(Output, ref Data, ref Offset, new byte[1] { 3 });
                        long ReferenceIndex = SerializedObjectList.IndexOf(CycleDetectionTable[Reference]);
                        AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(ReferenceIndex));
                    }
                    else
                    {
                        long Count = GetCollectionCount(Reference);
                        if (Count < 0)
                            AddField(Output, ref Data, ref Offset, new byte[1] { 1 });
                        else
                        {
                            AddField(Output, ref Data, ref Offset, new byte[1] { 2 });
                            AddField(Output, ref Data, ref Offset, BitConverter.GetBytes(Count));
                        }

                        AddSerializedObject(Reference, Count);
                    }
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

            object RootTypeNameValue;
            if (!DeserializeBasicType(Input, typeof(string), ref Data, ref Offset, out RootTypeNameValue) || RootTypeNameValue == null)
                return null;

            if (RootType == null)
            {
                string RootTypeName = RootTypeNameValue as string;
                RootType = DeserializedTrueType(RootTypeName);
            }

            object ModeValue;
            if (!DeserializeBasicType(Input, typeof(int), ref Data, ref Offset, out ModeValue))
                return null;

            Mode = (SerializationMode)ModeValue;

            DeserializedObjectList.Clear();

            object Reference;
            ProcessDeserializable(Input, RootType, ref Data, ref Offset, out Reference);

            Root = Reference;

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
                    object ConditionValue;
                    DeserializeBasicType(Input, typeof(bool), ref Data, ref Offset, out ConditionValue);
                    if (!(bool)ConditionValue)
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
                ReadField(Input, ref Data, ref Offset, CountByteSize);
                int CharCount = BitConverter.ToInt32(Data, Offset);
                Offset += CountByteSize;
                if (CharCount < 0)
                    Value = null;
                else
                {
                    ReadField(Input, ref Data, ref Offset, CharCount);
                    Value = Bytes2String(CharCount, Data, Offset);
                    Offset += CharCount * 2;
                }
            }

            else if (ValueType.IsEnum)
            {
                ReadField(Input, ref Data, ref Offset, 4);
                Value = BitConverter.ToInt32(Data, Offset);
                Offset += 4;
            }

            else
            {
                Value = null;
                return false;
            }

            return true;
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

            OverrideType(ref ReferenceType);

            if (ReferenceType.IsValueType)
            {
                CreateObject(ReferenceType, ref Reference);
                Deserialize(Input, ref Reference, ReferenceType, -1, ref Data, ref Offset, null);
            }

            else
            {
                ReadField(Input, ref Data, ref Offset, 1);
                byte ReferenceState = Data[Offset++];

                if (ReferenceState == 0)
                    Reference = null;

                else if (ReferenceState == 3)
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    int ReferenceIndex = (int)BitConverter.ToInt64(Data, Offset);
                    Offset += 8;

                    Reference = DeserializedObjectList[ReferenceIndex];
                }

                else if (ReferenceState == 1)
                {
                    CreateObject(ReferenceType, ref Reference);
                    AddDeserializedObject(Reference, ReferenceType, -1);
                }

                else if (ReferenceState == 2)
                {
                    ReadField(Input, ref Data, ref Offset, 8);
                    long Count = BitConverter.ToInt64(Data, Offset);
                    Offset += 8;

                    CreateObject(ReferenceType, Count, ref Reference);
                    AddDeserializedObject(Reference, ReferenceType, Count);
                }
            }
        }

        private void OverrideType(ref Type ReferenceType)
        {
            if (TypeOverrideTable.ContainsKey(ReferenceType))
                ReferenceType = TypeOverrideTable[ReferenceType];

            else
            {
                Assembly OldAssembly = ReferenceType.Assembly;
                if (AssemblyOverrideTable.ContainsKey(OldAssembly))
                {
                    Assembly NewAssembly = AssemblyOverrideTable[OldAssembly];

                    ReferenceType = NewAssembly.GetType(ReferenceType.FullName);

                    if (TypeOverrideTable.ContainsKey(ReferenceType))
                        ReferenceType = TypeOverrideTable[ReferenceType];
                }
            }
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
            long RemainingLength = (Input.Length - Input.Position);

            int Length;
            if (RemainingLength < Data.Length - Offset)
                Length = (int)RemainingLength;
            else
                Length = Data.Length - Offset;

            Input.Read(Data, Offset, Length);
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

        private bool IsSerializableMember(object Reference, Type SerializedType, SerializedMember NewMember)
        {
            if (NewMember.MemberInfo.MemberType != MemberTypes.Field && NewMember.MemberInfo.MemberType != MemberTypes.Property)
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
            {
                CharCount = -1;
                StringChars = new char[0];
            }
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
                SerializeBasicType(SerializedMembers.Count, typeof(int), ref Data, ref Offset);

                foreach (SerializedMember Member in SerializedMembers)
                    SerializeBasicType(Member.MemberInfo.Name, typeof(string), ref Data, ref Offset);
            }

            return SerializedMembers;
        }

        private List<DeserializedMember> ListDeserializedMembers(Stream Input, Type DeserializedType, ref byte[] Data, ref int Offset)
        {
            List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

            if (Mode == SerializationMode.MemberName)
            {
                object MemberCountObject;
                DeserializeBasicType(Input, typeof(int), ref Data, ref Offset, out MemberCountObject);

                int MemberCount = (int)MemberCountObject;

                for (int i = 0; i < MemberCount; i++)
                {
                    object MemberNameObject;
                    DeserializeBasicType(Input, typeof(string), ref Data, ref Offset, out MemberNameObject);

                    string MemberName = MemberNameObject as string;
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
