namespace PolySerializer
{
    using System;
    using System.Reflection;

    /// <summary>
    ///     Inserter for any class that supports the Add() method.
    /// </summary>
    public class GenericAddInserter : IInserter
    {
        /// <summary>
        ///     Description of the set of collections supported.
        /// </summary>
        public string Description { get { return "For generic types with an Add() method"; } }

        /// <summary>
        ///     List to which items will be added.
        /// </summary>
        public object Reference { get; private set; }

        /// <summary>
        ///     Method of the class called to add an item.
        /// </summary>
        public MethodInfo AddMethod { get; private set; }

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
            Type[] GenericArguments = referenceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgument = GenericArguments[0];
                MethodInfo SelectedAddMethod = null;

                MethodInfo[] MethodInfos = referenceType.GetMethods();
                foreach (MethodInfo MethodInfo in MethodInfos)
                    if (MethodInfo.Name == "Add")
                    {
                        ParameterInfo[] ParameterInfos = MethodInfo.GetParameters();
                        if (ParameterInfos.Length == 1)
                        {
                            ParameterInfo FirstParameterInfo = ParameterInfos[0];
                            if (FirstParameterInfo.ParameterType == GenericArgument)
                            {
                                SelectedAddMethod = MethodInfo;
                                break;
                            }
                        }
                    }

                if (SelectedAddMethod != null)
                {
                    Reference = reference;
                    AddMethod = SelectedAddMethod;
                    itemType = GenericArgument;
                    return true;
                }
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
            Type[] GenericArguments = referenceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgument = GenericArguments[0];
                MethodInfo SelectedAddMethod = null;

                MethodInfo[] MethodInfos = referenceType.GetMethods();
                foreach (MethodInfo MethodInfo in MethodInfos)
                    if (MethodInfo.Name == "Add")
                    {
                        ParameterInfo[] ParameterInfos = MethodInfo.GetParameters();
                        if (ParameterInfos.Length == 1)
                        {
                            ParameterInfo FirstParameterInfo = ParameterInfos[0];
                            if (FirstParameterInfo.ParameterType == GenericArgument)
                            {
                                SelectedAddMethod = MethodInfo;
                                break;
                            }
                        }
                    }

                if (SelectedAddMethod != null)
                {
                    AddMethod = SelectedAddMethod;
                    itemType = GenericArgument;
                    return true;
                }
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
            AddMethod.Invoke(Reference, new object[] { item });
        }
    }
}
