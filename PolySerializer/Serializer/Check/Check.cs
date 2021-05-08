namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
        /// <summary>
        /// Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// <returns>True of the stream can be deserialized, False otherwise.</returns>
        public bool Check(Stream input)
        {
            InitializeCheck(input);
            return INTERNAL_Check();
        }

        /// <summary>
        /// Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
            {
                HandleUTF8Indicator(Data, ref Offset);

                IsCheckedAsText = Data[Offset] == 'M' && Data[Offset + 1] == 'o' && Data[Offset + 2] == 'd' && Data[Offset + 3] == 'e';
            }
            else
            {
                IsCheckedAsText = Format == SerializationFormat.TextOnly;

                if (IsCheckedAsText)
                    HandleUTF8Indicator(Data, ref Offset);
            }

            if (IsCheckedAsText)
                return INTERNAL_Check_TEXT(ref Data, ref Offset);
            else
                return INTERNAL_Check_BINARY(ref Data, ref Offset);
        }

        private void HandleUTF8Indicator(byte[] data, ref int offset)
        {
            if (data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                offset += 3;
        }

        private void AddCheckedObject(Type checkedType, long count)
        {
            CheckedObjectList.Add(new CheckedObject(checkedType, count));
        }

        private List<ICheckedObject> CheckedObjectList = new List<ICheckedObject>();
    }
}
