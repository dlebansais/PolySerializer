using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PolySerializer
{
    public interface IInserter
    {
        string Description { get; }
        bool TrySetReference(object Reference, Type ReferenceType, out Type ItemType);
        void AddItem(object Item);
    }

    public class ArrayInserter : IInserter
    {
        public string Description { get { return "For arrays of any type (declared with the [] syntax)"; } }

        public bool TrySetReference(object Reference, Type ReferenceType, out Type ItemType)
        {
            Array AsArray;
            if ((AsArray = Reference as Array) != null)
            {
                this.Reference = AsArray;
                ItemType = ReferenceType.GetElementType();
                return true;
            }

            ItemType = null;
            return false;
        }

        public Array Reference { get; private set; }
        public int Index { get; private set; }

        public void AddItem(object Item)
        {
            Reference.SetValue(Item, Index++);
        }
    }

    public class ListInserter : IInserter
    {
        public string Description { get { return "For collections supporting both the IList and IList<T> interface"; } }

        public bool TrySetReference(object Reference, Type ReferenceType, out Type ItemType)
        {
            IList AsIList;
            if ((AsIList = Reference as IList) != null)
            {
                foreach (Type Interface in ReferenceType.GetInterfaces())
                {
                    if (Interface.IsGenericType)
                        if (Interface.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            Type[] GenericArguments = Interface.GetGenericArguments();
                            if (GenericArguments.Length > 0)
                            {
                                Type GenericArgument = GenericArguments[0];

                                this.Reference = AsIList;
                                ItemType = GenericArgument;
                                return true;
                            }
                        }
                }
            }

            ItemType = null;
            return false;
        }

        public IList Reference { get; private set; }

        public void AddItem(object Item)
        {
            Reference.Add(Item);
        }
    }

    public class GenericAddInserter : IInserter
    {
        public string Description { get { return "For generic types with an Add() method"; } }

        public bool TrySetReference(object Reference, Type ReferenceType, out Type ItemType)
        {
            Type[] GenericArguments = ReferenceType.GetGenericArguments();
            if (GenericArguments.Length > 0)
            {
                Type GenericArgument = GenericArguments[0];
                MethodInfo AddMethod = null;

                MethodInfo[] MethodInfos = ReferenceType.GetMethods();
                foreach (MethodInfo MethodInfo in MethodInfos)
                    if (MethodInfo.Name == "Add")
                    {
                        ParameterInfo[] ParameterInfos = MethodInfo.GetParameters();
                        if (ParameterInfos.Length == 1)
                        {
                            ParameterInfo FirstParameterInfo = ParameterInfos[0];
                            if (FirstParameterInfo.ParameterType == GenericArgument)
                            {
                                AddMethod = MethodInfo;
                                break;
                            }
                        }
                    }

                if (AddMethod != null)
                {
                    this.Reference = Reference;
                    this.AddMethod = AddMethod;
                    ItemType = GenericArgument;
                    return true;
                }
            }

            ItemType = null;
            return false;
        }

        public object Reference { get; private set; }
        public MethodInfo AddMethod { get; private set; }

        public void AddItem(object Item)
        {
            AddMethod.Invoke(Reference, new object[] { Item });
        }
    }
}
