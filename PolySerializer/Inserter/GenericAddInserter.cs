namespace PolySerializer
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Contracts;

    /// <summary>
    /// Inserter for any class that supports the Add() method.
    /// </summary>
    public class GenericAddInserter : IInserter
    {
        /// <summary>
        /// Gets the description of the set of collections supported.
        /// </summary>
        public string Description { get { return "For generic types with an Add() method"; } }

        /// <summary>
        /// Gets the list to which items will be added.
        /// </summary>
        public object Reference { get; private set; } = null!;

        /// <summary>
        ///  Gets the method of the class called to add an item.
        /// </summary>
        public MethodInfo AddMethod { get; private set; } = null!;

        /// <summary>
        /// Checks if <paramref name="reference"/> with base type <paramref name="referenceType"/> can be handled by this inserter.
        /// If so, the saves the reference for future calls to <see cref="AddItem"/> and returns the type of items for this collection.
        /// </summary>
        /// <param name="reference">The collection to check.</param>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// <returns>True if the inserter can handle the collection, false otherwise.</returns>
        public bool TrySetReference(object reference, Type referenceType, out Type itemType)
        {
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            Type[] GenericArguments = ReferenceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgumentType = GenericArguments[0];
                if (IsAddMethod(ReferenceType, GenericArgumentType, out MethodInfo SelectedAddMethod))
                {
                    Reference = reference;
                    AddMethod = SelectedAddMethod;
                    itemType = GenericArgumentType;
                    return true;
                }
            }

            Contract.Unused(out itemType);
            return false;
        }

        private bool IsAddMethod(Type referenceType, Type genericArgumentType, out MethodInfo selectedAddMethod)
        {
            MethodInfo[] MethodInfos = referenceType.GetMethods();
            foreach (MethodInfo MethodInfo in MethodInfos)
                if (MethodInfo.Name == "Add")
                    if (IsSingleParameterAddMethod(MethodInfo, genericArgumentType, out selectedAddMethod))
                        return true;

            Contract.Unused(out selectedAddMethod);
            return false;
        }

        private bool IsSingleParameterAddMethod(MethodInfo methodInfo, Type genericArgumentType, out MethodInfo selectedAddMethod)
        {
            ParameterInfo[] ParameterInfos = methodInfo.GetParameters();
            if (ParameterInfos.Length == 1)
            {
                ParameterInfo FirstParameterInfo = ParameterInfos[0];
                Debug.Assert(FirstParameterInfo.ParameterType == genericArgumentType);

                selectedAddMethod = methodInfo;
                return true;
            }

            Contract.Unused(out selectedAddMethod);
            return false;
        }

        /// <summary>
        /// Checks if base type <paramref name="referenceType"/> can be handled by this inserter.
        /// If so, returns the type of items for this collection.
        /// </summary>
        /// <param name="referenceType">The collection base type.</param>
        /// <param name="itemType">The type of items in the collection.</param>
        /// <returns>True if the inserter can handle the collection type, false otherwise.</returns>
        public bool TryMatchType(Type referenceType, out Type itemType)
        {
            Contract.RequireNotNull(referenceType, out Type ReferenceType);

            Type[] GenericArguments = ReferenceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgumentType = GenericArguments[0];
                if (IsAddMethod(ReferenceType, GenericArgumentType, out MethodInfo SelectedAddMethod))
                {
                    AddMethod = SelectedAddMethod;
                    itemType = GenericArgumentType;
                    return true;
                }
            }

            Contract.Unused(out itemType);
            return false;
        }

        /// <summary>
        /// Adds an item to the collection passed to <see cref="TrySetReference"/>.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(object? item)
        {
            AddMethod.Invoke(Reference, new object?[] { item });
        }
    }
}
