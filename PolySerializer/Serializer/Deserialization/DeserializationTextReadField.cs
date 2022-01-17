namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        private object ReadFieldSByte_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldByte_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldBool_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldChar_TEXT(ref byte[] data, ref int offset)
        {
            char Value;

            ReadField(ref data, ref offset, 2);
            offset++;

            string CharString = ReadStringUntil(ref data, ref offset, '\'');

            if ((CharString == "\\" && data.Length > offset && data[offset] == '\'') || CharString.Length == 0)
            {
                Value = '\'';
                offset++;
            }
            else
                Value = CharString[0];

            return Value;
        }

        private object ReadFieldDecimal_TEXT(ref byte[] data, ref int offset)
        {
            decimal Value;

            string s = ReadStringUntil(ref data, ref offset, 'm');
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Parsed))
                Value = Parsed;
            else
                Value = default(decimal);

            return Value;
        }

        private object ReadFieldDouble_TEXT(ref byte[] data, ref int offset)
        {
            double Value;

            string s = ReadStringUntil(ref data, ref offset, 'd');
            if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out double Parsed))
                Value = Parsed;
            else
                Value = default(double);

            return Value;
        }

        private object ReadFieldFloat_TEXT(ref byte[] data, ref int offset)
        {
            float Value;

            string s = ReadStringUntil(ref data, ref offset, 'f');
            if (float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out float Parsed))
                Value = Parsed;
            else
                Value = default(float);

            return Value;
        }

        private object ReadFieldInt_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldLong_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldShort_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldUInt_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldULong_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldUShort_TEXT(ref byte[] data, ref int offset)
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

        private object? ReadFieldString_TEXT(ref byte[] data, ref int offset)
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

            byte c = data[offset];

            if (c != '"')
            {
                offset++;
                return null;
            }

            string Content = string.Empty;

            offset++;

            int BaseOffset = offset;
            byte[] OldData = new byte[data.Length];
            Array.Copy(data, OldData, data.Length);

            while (true)
            {
                int OldOffset = offset;

                ReadField(ref data, ref offset, 1);

                if (OldOffset != offset)
                {
                    Content += Encoding.UTF8.GetString(OldData, BaseOffset, OldOffset - BaseOffset);
                    BaseOffset = offset;

                    OldData = new byte[data.Length];
                    Array.Copy(data, OldData, 0);
                }

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

            Content += Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            Value = Content.Replace("\\\"", "\"");

            return Value;
        }

        private object ReadFieldGuid_TEXT(ref byte[] data, ref int offset)
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

        private bool ReadFieldType_TEXT(ref byte[] data, ref int offset, out string? typeName)
        {
            ReadField(ref data, ref offset, 1);
            if (data[offset] != '{')
            {
                typeName = null;

                if (data[offset++] == 'n')
                {
                    ReadField(ref data, ref offset, 3);
                    if (data[offset + 0] == 'u' && data[offset + 1] == 'l' && data[offset + 2] == 'l')
                    {
                        offset += 3;
                        return true;
                    }
                }

                return false;
            }

            offset++;

            string Value = ReadStringUntil(ref data, ref offset, '}');

            typeName = Value;
            return true;
        }

        private List<string> ReadFieldMembers_TEXT(ref byte[] data, ref int offset)
        {
            List<string> MemberNames;

            int BaseOffset = offset;
            byte c;

            do
            {
                ReadField(ref data, ref offset, 1);
                offset++;

                c = data[offset - 1];
            }
            while (c != '\r' && c != '\n');

            string AllNames = Encoding.UTF8.GetString(data, BaseOffset, offset - BaseOffset - 1);
            if (AllNames.Length > 0)
            {
                string[] Splitted = AllNames.Split(',');
                MemberNames = new List<string>(Splitted);
            }
            else
                MemberNames = new List<string>();

            HandleCR(data, ref offset);

            return MemberNames;
        }

        private ObjectTag ReadFieldTag_TEXT(ref byte[] data, ref int offset)
        {
            ObjectTag Value;

            ReadField(ref data, ref offset, 1);
            char c = (char)data[offset++];

            if (c == '\r')
            {
                ReadField(ref data, ref offset, 1);
                c = (char)data[offset++];
            }

            Value = ObjectTag.ObjectReference;

            if (c == ' ')
            {
                ReadField(ref data, ref offset, 1);
                c = (char)data[offset++];

                if (c == '#')
                    Value = ObjectTag.ObjectIndex;
                else if (c == '!')
                    Value = ObjectTag.ConstructedObject;
                else if (c == '*')
                    Value = ObjectTag.ObjectList;
            }

            return Value;
        }

        private int ReadFieldObjectIndex_TEXT(ref byte[] data, ref int offset)
        {
            int Value;
            int BaseOffset = offset;
            byte c;

            do
            {
                ReadField(ref data, ref offset, 1);
                offset++;

                c = data[offset - 1];
            }
            while (c != '\r' && c != '\n');

            int n = 0;
            for (int i = BaseOffset; i + 1 < offset; i++)
                n = (n * 10) + FromDecimalDigit(data, i);

            Value = n;

            HandleCR(data, ref offset);

            return Value;
        }

        private long ReadFieldCount_TEXT(ref byte[] data, ref int offset)
        {
            long Value;
            int BaseOffset = offset;
            byte c;

            do
            {
                ReadField(ref data, ref offset, 1);
                offset++;

                c = data[offset - 1];
            }
            while (c != '\r' && c != '\n');

            long n = 0;
            for (int i = BaseOffset; i + 1 < offset; i++)
                n = (n * 10) + FromDecimalDigit(data, i);

            Value = n;

            HandleCR(data, ref offset);

            return Value;
        }

        private void ReadSeparator_TEXT(ref byte[] data, ref int offset)
        {
            ReadField(ref data, ref offset, 1);
            char c = (char)data[offset];
            offset++;
        }
    }
}
