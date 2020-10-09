namespace PolySerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Contracts;

    /// <summary>
    /// Represents a .NET type name.
    /// </summary>
    internal class TypeIdentifier
    {
        #region Init
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeIdentifier"/> class.
        /// </summary>
        /// <param name="typeName">The assembly qualified type name.</param>
        public TypeIdentifier(string typeName)
        {
            Name = typeName;

            if (typeName.Contains("[["))
            {
                DeconstructGenericTypeName(typeName, out string DeconstructedGenericDefinition, out List<TypeIdentifier> DeconstructedGenericParameters, out int _);
                GenericDefinition = DeconstructedGenericDefinition;
                GenericParameters = DeconstructedGenericParameters;
            }
            else
            {
                GenericDefinition = typeName;
                GenericParameters = new List<TypeIdentifier>();
            }

            Debug.Assert(ReconstructedTypeName() == Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeIdentifier"/> class.
        /// </summary>
        /// <param name="name">The assembly qualified type name.</param>
        /// <param name="genericDefinition">The generic definition, for a generic type; The type name for a non-generic type.</param>
        /// <param name="genericParameters">The generic parameters, for a generic type; The empty list a non-generic type.</param>
        private TypeIdentifier(string name, string genericDefinition, List<TypeIdentifier> genericParameters)
        {
            Name = name;
            GenericDefinition = genericDefinition;
            GenericParameters = genericParameters;

            Debug.Assert((!IsGeneric && Name == GenericDefinition && GenericParameters.Count == 0) || (IsGeneric && Name != GenericDefinition && GenericParameters.Count > 0));
        }
        #endregion

        #region Properties
        /// <summary>
        /// The assembly qualified type name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The generic definition, for a generic type; The type name for a non-generic type.
        /// </summary>
        public string GenericDefinition { get; private set; }

        /// <summary>
        /// The generic parameters, for a generic type; The empty list a non-generic type.
        /// </summary>
        public List<TypeIdentifier> GenericParameters { get; private set; }

        /// <summary>
        /// Indicates if the type is generic.
        /// </summary>
        public bool IsGeneric { get { return GenericParameters.Count > 0; } }
        #endregion

        #region Client Interface
        /// <summary>
        /// Override the type name and nested generic parameters using a search/replace.
        /// </summary>
        /// <param name="table">The search/replace table.</param>
        /// <param name="overrideGenericArguments">True to override generic parameters, otherwise the generic definition only.</param>
        /// <returns>True if a replacement was made; Otherwise, false.</returns>
        public bool Override(IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> table, bool overrideGenericArguments)
        {
            Contract.RequireNotNull(table, out IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> Table);

            bool IsOverriden = true;

            GenericDefinition = Override(Table, GenericDefinition, ref IsOverriden);

            if (overrideGenericArguments)
                foreach (TypeIdentifier GenericParameter in GenericParameters)
                    IsOverriden = GenericParameter.Override(Table, overrideGenericArguments);

            if (IsOverriden)
                Name = ReconstructedTypeName();

            return IsOverriden;
        }

        private static string Override(IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> table, string typeName, ref bool isOverriden)
        {
            isOverriden = false;

            foreach (KeyValuePair<NamespaceDescriptor, NamespaceDescriptor> Entry in table)
                if (NamespaceDescriptor.Match(typeName, Entry.Key, Entry.Value, out string TypeNameOverride))
                {
                    isOverriden = true;
                    return TypeNameOverride;
                }

            return typeName;
        }
        #endregion

        #region Type name parsing
        private void DeconstructGenericTypeName(string typeName, out string genericDefinition, out List<TypeIdentifier> genericParameters, out int lastIndex)
        {
            int StartIndex = typeName.IndexOf("[[", StringComparison.InvariantCulture);

            string Namespace = typeName.Substring(0, StartIndex);
            genericParameters = new List<TypeIdentifier>();

            StartIndex += 2;

            bool Exit = false;
            while (!Exit)
            {
                TypeIdentifier Parameter;
                string dbg = typeName.Substring(StartIndex);

                int ParseGeneric = IndexToEnd(typeName, "[[", StartIndex);
                int ParseNormal = IndexToEnd(typeName, "],[", StartIndex);
                int ParseLast = IndexToEnd(typeName, "]]", StartIndex);

                if (ParseLast < ParseGeneric && ParseLast < ParseNormal)
                {
                    string Name = typeName.Substring(StartIndex, ParseLast - StartIndex);
                    Parameter = new TypeIdentifier(Name);

                    StartIndex = ParseLast + 2;
                    Exit = true;
                }
                else if (ParseGeneric < ParseNormal)
                {
                    DeconstructGenericTypeName(typeName.Substring(StartIndex), out string GenericSubDefinition, out List<TypeIdentifier> GenericSubParameters, out int lastIndexSublist);
                    string Name = typeName.Substring(StartIndex, lastIndexSublist);
                    Parameter = new TypeIdentifier(Name, GenericSubDefinition, GenericSubParameters);

                    StartIndex += lastIndexSublist;

                    bool IsLast = typeName.Substring(StartIndex, 2) == "]]";
                    bool IsNotLast = typeName.Substring(StartIndex, 3) == "],[";
                    Debug.Assert(IsLast || IsNotLast);

                    if (IsLast)
                    {
                        StartIndex += 2;
                        Exit = true;
                    }
                    else
                        StartIndex += 3;
                }
                else
                {
                    string Name = typeName.Substring(StartIndex, ParseNormal - StartIndex);
                    Parameter = new TypeIdentifier(Name);

                    StartIndex = ParseNormal + 3;
                }

                genericParameters.Add(Parameter);
            }

            lastIndex = IndexToEnd(typeName, "]", StartIndex);
            string Remaining = typeName.Substring(StartIndex, lastIndex - StartIndex);

            genericDefinition = Namespace + Remaining;
        }

        private static int IndexToEnd(string text, string pattern, int startIndex)
        {
            int Index = text.IndexOf(pattern, startIndex, StringComparison.InvariantCulture);
            if (Index < 0)
                Index = text.Length;

            return Index;
        }

        private string ReconstructedTypeName()
        {
            if (GenericParameters.Count == 0)
                return GenericDefinition;
            else
            {
                int AssemblyIndex = GenericDefinition.IndexOf(",", StringComparison.InvariantCulture);

                string Parameters = string.Empty;

                foreach (TypeIdentifier GenericParameter in GenericParameters)
                {
                    if (Parameters.Length > 0)
                        Parameters += "],[";

                    Parameters += GenericParameter.Name;
                }

                return GenericDefinition.Substring(0, AssemblyIndex) + "[[" + Parameters + "]]" + GenericDefinition.Substring(AssemblyIndex);
            }
        }
        #endregion

#if DEBUG
#pragma warning disable CA1823 // Avoid unused private fields
        private static readonly TypeIdentifier Test0 = new TypeIdentifier(typeof(string).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test1 = new TypeIdentifier(typeof(List<string>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test2 = new TypeIdentifier(typeof(Dictionary<string, string>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test3 = new TypeIdentifier(typeof(List<List<string>>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test4 = new TypeIdentifier(typeof(List<Dictionary<string, string>>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test5 = new TypeIdentifier(typeof(Dictionary<string, List<string>>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test6 = new TypeIdentifier(typeof(Dictionary<string, Dictionary<string, string>>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test7 = new TypeIdentifier(typeof(Dictionary<List<string>, string>).AssemblyQualifiedName !);
        private static readonly TypeIdentifier Test8 = new TypeIdentifier(typeof(Dictionary<Dictionary<string, string>, string>).AssemblyQualifiedName !);
#pragma warning restore CA1823 // Avoid unused private fields
#endif
    }
}
