namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Contracts;

    /// <summary>
    ///     Inserter for lists.
    /// </summary>
    public class ListInserter : IInserter
    {
        /// <summary>
        ///     Description of the set of collections supported.
        /// </summary>
        public string Description { get { return "For collections supporting both the IList and IList<T> interface"; } }

        /// <summary>
        ///     List to which items will be added.
        /// </summary>
        public IList Reference { get; private set; } = null!;

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
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            if (reference is IList AsIList)
                return TrySetListReference(AsIList, ReferenceType, out itemType);

            Contract.Unused(out itemType);
            return false;
        }

        private bool TrySetListReference(IList listReference, Type referenceType, out Type itemType)
        {
            foreach (Type Interface in referenceType.GetInterfaces())
                if (Interface.IsGenericType)
                    if (Interface.GetGenericTypeDefinition() == typeof(IList<>))
                        if (TrySetGenericListReference(listReference, Interface, out itemType))
                            return true;

            Contract.Unused(out itemType);
            return false;
        }

        private bool TrySetGenericListReference(IList listReference, Type interfaceType, out Type itemType)
        {
            Debug.Assert(interfaceType.IsGenericType);
            Debug.Assert(interfaceType.GetGenericTypeDefinition() == typeof(IList<>));

            Type[] GenericArguments = interfaceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgument = GenericArguments[0];

                Reference = listReference;
                itemType = GenericArgument;
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

            foreach (Type Interface in ReferenceType.GetInterfaces())
                if (Interface.IsGenericType)
                    if (Interface.GetGenericTypeDefinition() == typeof(IList<>))
                        if (TryMatchGenericType(Interface, out itemType))
                            return true;

            Contract.Unused(out itemType);
            return false;
        }

        private bool TryMatchGenericType(Type interfaceType, out Type itemType)
        {
            Debug.Assert(interfaceType.IsGenericType);
            Debug.Assert(interfaceType.GetGenericTypeDefinition() == typeof(IList<>));

            Type[] GenericArguments = interfaceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgument = GenericArguments[0];

                itemType = GenericArgument;
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
            Reference.Add(item);
        }
    }
}
