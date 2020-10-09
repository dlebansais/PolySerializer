namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     Serialize objects to a stream, or deserialize objects from a stream.
    /// </summary>
    public partial class Serializer : ISerializer
    {
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
            Type SerializedType = SerializableAncestor(reference.GetType()) !;
            SerializableObject NewSerialized = new SerializableObject(reference, SerializedType, count);
            SerializedObjectList.Add(NewSerialized);

            CycleDetectionTable.Add(reference, NewSerialized);
        }

        private static long GetCollectionCount(object reference)
        {
            long Count = 0;
            if (reference is IEnumerable AsEnumerable)
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
            Stream OutputStream = Output!;

            if (offset + content.Length > data.Length)
            {
                OutputStream.Write(data, 0, offset);
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

        private List<ISerializableObject> SerializedObjectList = new List<ISerializableObject>();
        private Dictionary<object, SerializableObject> CycleDetectionTable = new Dictionary<object, SerializableObject>();
    }
}
