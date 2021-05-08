[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test-PolySerializer")]

namespace PolySerializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Contracts;

    /// <summary>
    /// Public interface of the serializer.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Gets or sets how objects are serialized and deserialized.
        /// </summary>
        SerializationMode Mode { get; set; }

        /// <summary>
        /// Gets or sets how objects are serialized and deserialized.
        /// </summary>
        SerializationFormat Format { get; set; }

        /// <summary>
        /// Gets the output stream on which serialized data has been written to in <see cref="Serialize"/>.
        /// </summary>
        Stream? Output { get; }

        /// <summary>
        /// Gets the object serialized (after a call to <see cref="Serialize"/>) or created (after a call to <see cref="Deserialize"/>).
        /// </summary>
        object? Root { get; }

        /// <summary>
        /// Gets the input stream from which deserialized data has been read from in <see cref="Deserialize"/>.
        /// </summary>
        Stream? Input { get; }

        /// <summary>
        /// Gets or sets the Type of the <see cref="Root"/> object after a call to <see cref="Serialize"/>, or type of the object to create in <see cref="Deserialize"/>.
        /// If null, <see cref="Deserialize"/> finds the type to use from the serialized data. If not null, the serialized data must be compatible with this type or <see cref="Deserialize"/> will throw an exception.
        /// </summary>
        Type? RootType { get; set; }

        /// <summary>
        /// Gets the serialization or deserialization progress as a number between 0 and 1.
        /// </summary>
        double Progress { get; }

        /// <summary>
        /// Gets or sets a list of assemblies that can override the original assembly of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Assembly, Assembly> AssemblyOverrideTable { get; set; }

        /// <summary>
        /// Gets or sets a list of namespaces that can override the original namespace of a type during deserialization.
        /// </summary>
        IReadOnlyDictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable { get; set; }

        /// <summary>
        /// Gets or sets a list of types that can override the original type during deserialization.
        /// </summary>
        IReadOnlyDictionary<Type, Type> TypeOverrideTable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether arguments of generic types should be overriden.
        /// </summary>
        bool OverrideGenericArguments { get; set; }

        /// <summary>
        /// Gets or sets a list of inserter objects that allow filling collection of items implemented using a custom type, or a type not natively supported (<seealso cref="BuiltInInserters"/>.
        /// </summary>
        IReadOnlyList<IInserter> CustomInserters { get; set; }

        /// <summary>
        /// Gets a list of inserters that can add items to various types of collections.
        /// </summary>
        IReadOnlyList<IInserter> BuiltInInserters { get; }

        /// <summary>
        /// Serializes <paramref name="root"/> and write the serialized data to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        void Serialize(Stream output, object root);

        /// <summary>
        /// Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(Stream input);

        /// <summary>
        /// Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// <returns>True of the stream can be deserialized, False otherwise.</returns>
        bool Check(Stream input);

        /// <summary>
        /// Serializes <paramref name="root"/> and write the serialized data to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">Stream receiving the serialized data.</param>
        /// <param name="root">Serialized object.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeAsync(Stream output, object root);

        /// <summary>
        /// Creates a new object from serialized content in <paramref name="input"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to create the new object.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<object> DeserializeAsync(Stream input);

        /// <summary>
        /// Checks if serialized data in <paramref name="input"/> is compatible with <see cref="RootType"/>.
        /// </summary>
        /// <param name="input">Stream from which serialized data is read to check for compatibility.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<bool> CheckAsync(Stream input);
    }
}
