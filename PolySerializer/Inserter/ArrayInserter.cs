namespace PolySerializer
{
    using System;
    using Contracts;

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
        public Array Reference { get; private set; } = null !;

        /// <summary>
        ///     Index of the slow where the next item will be inserted.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        ///     Checks if <paramref name="reference"/> with base type <paramref name="referenceType"/> can be handled by this inserter.
        ///     If so, saves the reference for future calls to <see cref="AddItem"/> and returns the type of items for this collection.
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
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            if (reference is Array AsArray)
            {
                Reference = AsArray;
                itemType = ReferenceType.GetElementType() !;
                return true;
            }

            Contract.Unused(out itemType);
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
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            if (ReferenceType.IsArray)
            {
                itemType = ReferenceType.GetElementType() !;
                return true;
            }

            Contract.Unused(out itemType);
            return false;
        }

        /// <summary>
        ///     Adds an item to the collection passed to <see cref="TrySetReference"/>.
        /// </summary>
        /// <parameters>
        /// <param name="item">The item to add.</param>
        /// </parameters>
        public void AddItem(object? item)
        {
            Reference.SetValue(item, Index++);
        }
    }
}
