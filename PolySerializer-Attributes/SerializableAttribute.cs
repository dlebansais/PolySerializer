namespace PolySerializer;

using System;

/// <summary>
/// Attribute to enable serialization, or to specify how to serialize a member.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class SerializableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the member should be excluded from serialization (and deserialization).
    /// </summary>
    public bool Exclude { get; set; } = false;

    /// <summary>
    /// Gets or sets a condition for deserializing a member.
    /// If the boolean member indicated by <see cref="Condition"/> is set to true, the member with this attribute is serialized, otherwise it is ignored and won't be deserialized.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a setter for deserializing a member.
    /// Applies only to read-only properties.
    /// If the member indicated by <see cref="Setter"/> is a method taking only one argument of the same type, the method is called with the value to deserialize.
    /// </summary>
    public string Setter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of property values to use to construct a deserialized object.
    /// Applies only to one of the object's constructors.
    /// If this attribute isn't specified, the parameterless constructor is used.
    /// If specified, the constructor must have parameters, their name must match properties of the object's type, and this attribute must list them as a comma-separated list. Serialized values of these properties are used in the same order when calling the constructor.
    /// If the deserialized type is different than the serialized type, both types must have this attribute set on compatible constructors.
    /// </summary>
    public string Constructor { get; set; } = string.Empty;
}
