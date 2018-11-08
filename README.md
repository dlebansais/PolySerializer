# Status: alpha
Please wait a few days while I sort out tests of core features and optimize. Serialized data generated today may not be backward-compatible tomorrow.

TODO:
- [x] Missing: Conversion to human-readable text.
- [ ] Missing: Checking validity of serialized streams and providing diagnostic information.
- [X] Confirm support of very large collections (several billions items).
- [X] Support of constructors with parameters.
- [X] Missing: Asynchronous operations.
- [ ] Missing: Conversion to different endianness.
- [ ] Optimization.
- [ ] Doc and example.

# Polymorphic Serializer
A generic .NET serializer for object exchange between assemblies. Supports polymorphism and much more.

![Build Status](https://img.shields.io/travis/dlebansais/PolySerializer/master.svg)

PolySerializer takes C# objects and serializes them (i.e. copy them onto a stream that can be archived or exchanged). Contrary to most serializers, it will locate the first parent in the inheritance tree that supports serialization, and only serializes object members belonging to that parent. Likewise, when the object is deserialized (i.e. read from a stream and transformed into an actual, live C# object) the destination type can be chosen so that the type of the created object is another descendant of the serialized type, possibly different than the original. Only members of the destination will then be initialized to their serialized value. See the example section to better understand how it works.

## Features
PolySerializer supports the following features:
* Partial serialization and deserialization of objects by their common serializable parent type. Even with different assemblies.
* Binary output of either or both the object data or format. This allows saving many objects on one stream and their format once on another.
* Conversion to human-readable text, using either data and format from the same stream, or data from one stream and format from another.
* Format conversion between types that have identical binary representation but different member names. This can be used to rename members in a type without loosing archived objects. Deserializing to a type with new members (initialized to their default value) is also supported.
* Deserialization test to check if deserializing a stream would produce an exception.
* Flexible collection recording: only objects in the collection are serialized, allowing interoperability between collection types (list, stacks, queues...). Only some form of GetNextItem and AddItem support being required.
* Serialization and deserialization of read-only members, so that types can expose a read-only interface to the program and still support being deserialized.
* Mutable members can be excluded. They can also be excluded only if a condition is false (this is useful in some scenarios).
* Support of contructors with parameters for deserialized objects.
* Version tolerance: a full version identifier of the assembly implementing the serialized type can be saved in the output, and analyzed later to determine which assembly to use for deserialization or conversion.
* Support of very large objects.
* Asynchronous serialization/deserialization with progress, enabling previsualization of the deserialized object before completion.
* Interoperability between platforms through conversion (see below).
* Support for cyclic structures (for example, children linking to parents).

## Interoperability between platforms
Very important: the binary format used doesn't allow exchange between platforms with different endianness or pointer size. To obtain that, either use another serializer more suited to platform-agnostic data exchange, or use the conversion feature of PolySerializer that let you transform data for object serialized for one platform to data serialized for another platform. This operation is not optimized for performance and does not support many of PolySerializer features (such as flexible collections, previsualization). But it does the job.

## Performance


