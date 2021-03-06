﻿namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
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

        private object ReadFieldDecimal_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldDouble_TEXT(ref byte[] data, ref int offset)
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

        private object ReadFieldFloat_TEXT(ref byte[] data, ref int offset)
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

        private string? ReadFieldType_TEXT(ref byte[] data, ref int offset)
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

            if (c == '\r')
                offset++;

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

            if (c == '\r')
                offset++;

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

            if (c == '\r')
                offset++;

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
