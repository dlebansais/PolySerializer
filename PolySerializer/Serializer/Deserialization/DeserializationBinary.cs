﻿namespace PolySerializer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Contracts;

/// <summary>
/// Serialize objects to a stream, or deserialize objects from a stream.
/// </summary>
public partial class Serializer : ISerializer
{
    private object INTERNAL_Deserialize_BINARY(ref byte[] data, ref int offset)
    {
        Mode = (SerializationMode)BitConverter.ToInt32(data, offset);
        offset += 4;

        DeserializedObjectList.Clear();

        ProcessDeserializable_BINARY(RootType, ref data, ref offset, out object? Reference);

        Root = Reference!;

        if (RootType is null)
            RootType = Root.GetType();

        int i = 0;
        while (i < DeserializedObjectList.Count)
        {
            Progress = i / (double)DeserializedObjectList.Count;

            IDeserializedObject NextDeserialized = DeserializedObjectList[i++];
            Reference = NextDeserialized.Reference;
            Deserialize_BINARY(Reference, NextDeserialized.DeserializedType, NextDeserialized.Count, ref data, ref offset, NextDeserialized);
        }

        Progress = 1.0;

        return Root;
    }

    private void Deserialize_BINARY(object reference, Type referenceType, long count, ref byte[] data, ref int offset, IDeserializedObject? nextDeserialized)
    {
        DeserializeCollection_BINARY(reference, referenceType, count, ref data, ref offset);

        Type DeserializedType = SerializableAncestor(referenceType)!;
        List<DeserializedMember> DeserializedMembers = ListDeserializedMembers_BINARY(DeserializedType, ref data, ref offset);

        foreach (DeserializedMember Member in DeserializedMembers)
        {
            if (Member.HasCondition)
            {
                bool ConditionValue = (bool)ReadFieldBool_BINARY(ref data, ref offset);
                if (!ConditionValue)
                    continue;
            }

            Type MemberType;

            if (Member.MemberInfo is FieldInfo AsFieldInfoBefore)
                MemberType = AsFieldInfoBefore.FieldType;
            else
            {
                PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                MemberType = AsPropertyInfo.PropertyType;
            }

            ProcessDeserializable_BINARY(MemberType, ref data, ref offset, out object? MemberValue);

            if (Member.MemberInfo is FieldInfo AsFieldInfoAfter)
                AsFieldInfoAfter.SetValue(reference, MemberValue);
            else
            {
                PropertyInfo AsPropertyInfo = (PropertyInfo)Member.MemberInfo;
                if (Member.PropertySetter is null)
                    AsPropertyInfo.SetValue(reference, MemberValue);
                else
                    Member.PropertySetter.Invoke(reference, new object?[] { MemberValue });
            }
        }

        if (nextDeserialized is not null)
            nextDeserialized.SetDeserialized();
    }

    private void DeserializeCollection_BINARY(object reference, Type referenceType, long count, ref byte[] data, ref int offset)
    {
        if (count >= 0)
        {
            if (IsWriteableCollection(reference, referenceType, out IInserter Inserter, out Type ItemType))
            {
                for (long i = 0; i < count; i++)
                {
                    if (!ProcessDeserializable_BINARY(ItemType, ref data, ref offset, out object? Item))
                        break;

                    Inserter.AddItem(Item);
                }
            }
        }
    }

    private delegate object? ReadFieldHandler_BINARY(ref byte[] data, ref int offset);
    private IDictionary<string, ReadFieldHandler_BINARY> ReadFieldHandlerTable_BINARY { get; set; } = new Dictionary<string, ReadFieldHandler_BINARY>();

    private void IniReadFieldHandlerTable_BINARY()
    {
        if (ReadFieldHandlerTable_BINARY.Count == 0)
            ReadFieldHandlerTable_BINARY = new Dictionary<string, ReadFieldHandler_BINARY>()
            {
                { nameof(SByte), ReadFieldSByte_BINARY },
                { nameof(Byte), ReadFieldByte_BINARY },
                { nameof(Boolean), ReadFieldBool_BINARY },
                { nameof(Char), ReadFieldChar_BINARY },
                { nameof(Decimal), ReadFieldDecimal_BINARY },
                { nameof(Double), ReadFieldDouble_BINARY },
                { nameof(Single), ReadFieldFloat_BINARY },
                { nameof(Int32), ReadFieldInt_BINARY },
                { nameof(Int64), ReadFieldLong_BINARY },
                { nameof(Int16), ReadFieldShort_BINARY },
                { nameof(UInt32), ReadFieldUInt_BINARY },
                { nameof(UInt64), ReadFieldULong_BINARY },
                { nameof(UInt16), ReadFieldUShort_BINARY },
                { nameof(String), ReadFieldString_BINARY },
                { nameof(Guid), ReadFieldGuid_BINARY },
            };
    }

    private bool DeserializeBasicType_BINARY(Type? valueType, ref byte[] data, ref int offset, out object? value)
    {
        IniReadFieldHandlerTable_BINARY();

        string? ValueName = valueType?.Name;

        if (ValueName is not null && ReadFieldHandlerTable_BINARY.ContainsKey(ValueName))
        {
            value = ReadFieldHandlerTable_BINARY[ValueName](ref data, ref offset);
            return true;
        }
        else if (valueType is not null && valueType.IsEnum)
        {
            DeserializeEnumType_BINARY(valueType, ref data, ref offset, out value);
            return true;
        }
        else
        {
            value = null;
            return false;
        }
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
            default:
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
        }
    }

    private bool ProcessDeserializable_BINARY(Type? referenceType, ref byte[] data, ref int offset, out object? reference)
    {
        if (DeserializeBasicType_BINARY(referenceType, ref data, ref offset, out reference))
            return true;

        string? ReferenceTypeName = ReadFieldType_BINARY(ref data, ref offset);
        if (ReferenceTypeName is null)
        {
            reference = null;
            return false;
        }

        OverrideTypeName(ref ReferenceTypeName);

        Type? ReferenceType;

        try
        {
            ReferenceType = Type.GetType(ReferenceTypeName);
        }
        catch (SystemException)
        {
            ReferenceType = null;
        }

        if (ReferenceType is null)
        {
            reference = null;
            return false;
        }

        Type OriginalType = ReferenceType;
        OverrideType(ref ReferenceType);
        Type NewType = ReferenceType;
        ReferenceType = OriginalType;

        if (ReferenceType.IsValueType)
            return ProcessDeserializableValue_BINARY(NewType, ReferenceType, ref data, ref offset, out reference);
        else
            return ProcessDeserializableReference_BINARY(NewType, ReferenceType, ref data, ref offset, out reference);
    }

    private bool ProcessDeserializableValue_BINARY(Type objectType, Type referenceType, ref byte[] data, ref int offset, out object? reference)
    {
        CreateObject(objectType, out reference);
        Deserialize_BINARY(reference, referenceType, -1, ref data, ref offset, null);

        return true;
    }

    private bool ProcessDeserializableReference_BINARY(Type objectType, Type referenceType, ref byte[] data, ref int offset, out object? reference)
    {
        ObjectTag ReferenceTag = ReadFieldTag_BINARY(ref data, ref offset);

        if (ReferenceTag == ObjectTag.ObjectIndex)
        {
            int ReferenceIndex = ReadFieldObjectIndex_BINARY(ref data, ref offset);
            reference = DeserializedObjectList[ReferenceIndex].Reference;
        }
        else if (ReferenceTag == ObjectTag.ObjectList)
        {
            long Count = ReadFieldCount_BINARY(ref data, ref offset);

            CreateObject(objectType, Count, out reference);
            AddDeserializedObject(reference, referenceType, Count);
        }
        else if (ReferenceTag == ObjectTag.ConstructedObject)
        {
            bool HasSerializableConstructor = ListConstructorParameters(referenceType, out List<SerializedMember> ConstructorParameters);
            Debug.Assert(HasSerializableConstructor);

            object?[] Parameters = new object?[ConstructorParameters.Count];

            for (int i = 0; i < ConstructorParameters.Count; i++)
            {
                PropertyInfo AsPropertyInfo = (PropertyInfo)ConstructorParameters[i].MemberInfo;

                Type MemberType = AsPropertyInfo.PropertyType;
                if (!ProcessDeserializable_BINARY(MemberType, ref data, ref offset, out object? MemberValue))
                {
                    reference = null;
                    return false;
                }

                Parameters[i] = MemberValue;
            }

            CreateObject(objectType, Parameters, out reference);
            AddDeserializedObject(reference, referenceType, -1);
        }
        else if (ReferenceTag == ObjectTag.ObjectReference)
        {
            CreateObject(objectType, out reference);
            AddDeserializedObject(reference, referenceType, -1);
        }
        else
        {
            reference = null;
            return false;
        }

        return true;
    }

    private List<DeserializedMember> ListDeserializedMembers_BINARY(Type deserializedType, ref byte[] data, ref int offset)
    {
        List<DeserializedMember> DeserializedMembers = new List<DeserializedMember>();

        if (Mode == SerializationMode.MemberName)
        {
            List<string> MemberNames = ReadFieldMembers_BINARY(ref data, ref offset);
            foreach (string MemberName in MemberNames)
            {
                MemberInfo[] MatchingMembers = deserializedType.GetMember(MemberName)!;
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
}
