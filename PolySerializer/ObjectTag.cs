namespace PolySerializer
{
    public enum ObjectTag : byte
    {
        Reserved,
        ObjectReference,   // Normal object
        ObjectList,        // Collection of objects
        ObjectIndex,       // The object has already been serialized (cyclic reference)
        ConstructedObject, // Normal object with a constructor taking parameters
    }
}
