namespace PolySerializer
{
#pragma warning disable SA1602 // Enumeration items must be documented
    internal enum ObjectTag : byte
    {
        Reserved,
        ObjectReference,   // Normal object
        ObjectList,        // Collection of objects
        ObjectIndex,       // The object has already been serialized (cyclic reference)
        ConstructedObject, // Normal object with a constructor taking parameters
    }
#pragma warning restore SA1602 // Enumeration items must be documented
}
