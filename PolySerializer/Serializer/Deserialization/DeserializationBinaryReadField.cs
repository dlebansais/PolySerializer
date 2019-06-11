namespace PolySerializer
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private object ReadFieldSByte_BINARY(ref byte[] data, ref int offset)
        {
            sbyte Value;

            ReadField(ref data, ref offset, 1);
            Value = (sbyte)data[offset];
            offset++;

            return Value;
        }

        private object ReadFieldByte_BINARY(ref byte[] data, ref int offset)
        {
            byte Value;

            ReadField(ref data, ref offset, 1);
            Value = data[offset];
            offset++;

            return Value;
        }

        private object ReadFieldBool_BINARY(ref byte[] data, ref int offset)
        {
            bool Value;

            ReadField(ref data, ref offset, 1);
            Value = BitConverter.ToBoolean(data, offset);
            offset += 1;

            return Value;
        }

        private object ReadFieldChar_BINARY(ref byte[] data, ref int offset)
        {
            char Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToChar(data, offset);
            offset += 2;

            return Value;
        }

        private object ReadFieldDecimal_BINARY(ref byte[] data, ref int offset)
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

        private object ReadFieldDouble_BINARY(ref byte[] data, ref int offset)
        {
            double Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToDouble(data, offset);
            offset += 8;

            return Value;
        }

        private object ReadFieldFloat_BINARY(ref byte[] data, ref int offset)
        {
            float Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToSingle(data, offset);
            offset += 4;

            return Value;
        }

        private object ReadFieldInt_BINARY(ref byte[] data, ref int offset)
        {
            int Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToInt32(data, offset);
            offset += 4;

            return Value;
        }

        private object ReadFieldLong_BINARY(ref byte[] data, ref int offset)
        {
            long Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToInt64(data, offset);
            offset += 8;

            return Value;
        }

        private object ReadFieldShort_BINARY(ref byte[] data, ref int offset)
        {
            short Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToInt16(data, offset);
            offset += 2;

            return Value;
        }

        private object ReadFieldUInt_BINARY(ref byte[] data, ref int offset)
        {
            uint Value;

            ReadField(ref data, ref offset, 4);
            Value = BitConverter.ToUInt32(data, offset);
            offset += 4;

            return Value;
        }

        private object ReadFieldULong_BINARY(ref byte[] data, ref int offset)
        {
            ulong Value;

            ReadField(ref data, ref offset, 8);
            Value = BitConverter.ToUInt64(data, offset);
            offset += 8;

            return Value;
        }

        private object ReadFieldUShort_BINARY(ref byte[] data, ref int offset)
        {
            ushort Value;

            ReadField(ref data, ref offset, 2);
            Value = BitConverter.ToUInt16(data, offset);
            offset += 2;

            return Value;
        }

        private object ReadFieldString_BINARY(ref byte[] data, ref int offset)
        {
            string Value;

            string StringValue;
            ReadStringField(ref data, ref offset, out StringValue);
            Value = StringValue;

            return Value;
        }

        private object ReadFieldGuid_BINARY(ref byte[] data, ref int offset)
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

            Value = (long)ReadFieldLong_BINARY(ref data, ref offset);

            return Value;
        }
    }
}
