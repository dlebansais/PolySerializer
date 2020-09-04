namespace PolySerializer
{
    using System;

    /// <summary>
    ///     Inserter for arrays.
    /// </summary>
    public class ArrayInserter : IInserter
    {
        /// <summary>
        ///     Description of the set of collections supported.
        /// </summary>
        public string Description { get { return "For arrays of any type (declared with the [] syntax)"; } }

        /// <summary>
        ///     Array to which items will be added.
        /// </summary>
        public Array Reference { get; private set; }

        /// <summary>
        ///     Index of the slow where the next item will be inserted.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        ///     Checks if <paramref name="reference"/> with base type <paramref name="referenceType"/> can be handled by this inserter.
        ///     If so, the saves the reference for future calls to <see cref="AddItem"/> and returns the type of items for this collection.
        /// </summary>
        /// <parameters>
        /// <param name="reference">The collection to check.</param>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// </parameters>
        /// <returns>
        ///     True if the inserter can handle the collection, false otherwise.
        /// </returns>
        public bool TrySetReference(object reference, Type referenceType, out Type itemType)
        {
            if (reference is Array AsArray)
            {
                if (referenceType == null)
                    throw new ArgumentNullException(nameof(referenceType));

                Reference = AsArray;
                itemType = referenceType.GetElementType();
                return true;
            }

            itemType = null;
            return false;
        }

        /// <summary>
        ///     Checks if base type <paramref name="referenceType"/> can be handled by this inserter.
        ///     If so, returns the type of items for this collection.
        /// </summary>
        /// <parameters>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// </parameters>
        /// <returns>
        ///     True if the inserter can handle the collection type, false otherwise.
        /// </returns>
        public bool TryMatchType(Type referenceType, out Type itemType)
        {
            if (referenceType == null)
                throw new ArgumentNullException(nameof(referenceType));

            if (referenceType.IsArray)
            {
                itemType = referenceType.GetElementType();
                return true;
            }

            itemType = null;
            return false;
        }

        /// <summary>
        ///     Adds an item to the collection passed to <see cref="TrySetReference"/>.
        /// </summary>
        /// <parameters>
        /// <param name="item">The item to add.</param>
        /// </parameters>
        public void AddItem(object item)
        {
            Reference.SetValue(item, Index++);
        }
    }
}
