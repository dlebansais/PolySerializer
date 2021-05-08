namespace PolySerializer
{
    using System;

    /// <summary>
    /// Public interface of an inserter.
    /// </summary>
    public interface IInserter
    {
        /// <summary>
        /// Gets the description of the set of collections supported.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Checks if <paramref name="reference"/> with base type <paramref name="referenceType"/> can be handled by this inserter.
        /// If so, the saves the reference for future calls to <see cref="AddItem"/> and returns the type of items for this collection.
        /// </summary>
        /// <param name="reference">The collection to check.</param>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// <returns>True if the inserter can handle the collection, false otherwise.</returns>
        bool TrySetReference(object reference, Type referenceType, out Type itemType);

        /// <summary>
        /// Checks if base type <paramref name="referenceType"/> can be handled by this inserter.
        /// If so, returns the type of items for this collection.
        /// </summary>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// <returns>True if the inserter can handle the collection type, false otherwise.</returns>
        bool TryMatchType(Type referenceType, out Type itemType);

        /// <summary>
        /// Adds an item to the collection passed to <see cref="TrySetReference"/>.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void AddItem(object? item);
    }
}
