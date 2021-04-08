# Polymorphic Serializer
A generic .NET serializer for object exchange between assemblies. Supports polymorphism and much more.

[![Build status](https://ci.appveyor.com/api/projects/status/va48foddbbydxiov?svg=true)](https://ci.appveyor.com/project/dlebansais/polyserializer-khj0v)
[![codecov](https://codecov.io/gh/dlebansais/PolySerializer/branch/master/graph/badge.svg?token=gDmPN1mYLj)](https://codecov.io/gh/dlebansais/PolySerializer)
[![CodeFactor](https://www.codefactor.io/repository/github/dlebansais/polyserializer/badge)](https://www.codefactor.io/repository/github/dlebansais/polyserializer)

PolySerializer takes C# objects and serializes them (i.e. copy them onto a stream that can be archived or exchanged). Contrary to most serializers, it will locate the first parent in the inheritance tree that supports serialization, and only serializes object members belonging to that parent. Likewise, when the object is deserialized (i.e. read from a stream and transformed into an actual, live C# object) the destination type can be chosen so that the type of the created object is another descendant of the serialized type, possibly different than the original. Only members of the destination will then be initialized to their serialized value. See the example section to better understand how it works.

## Features
PolySerializer supports the following features:
* Partial serialization and deserialization of objects by their common serializable parent type. Even with different assemblies.
* Binary output of either or both the object data or format. This allows saving many objects on one stream and their format once on another (**this is not implemented yet**).
* Using human-readable text instead of binary data. This is much slower, but can be useful for searches.
* Format conversion between types that have identical binary representation but different member names. This can be used to rename members in a type without loosing archived objects. Deserializing to a type with new members (initialized to their default value) is also supported.
* Deserialization test to check if deserializing a stream would produce an exception.
* Flexible collection recording: only objects in the collection are serialized, allowing interoperability between collection types (list, stacks, queues...). Only some form of GetNextItem and AddItem support being required.
* Serialization and deserialization of read-only members, so that types can expose a read-only interface to the program and still support being deserialized.
* Mutable members can be excluded. They can also be excluded only if a condition is false (this is useful in some scenarios).
* Support of constructors with parameters for deserialized objects.
* Version tolerance: a full version identifier of the assembly implementing the serialized type can be saved in the output, and analyzed later to determine which assembly to use for deserialization or conversion.
* Support of very large objects.
* Asynchronous serialization/deserialization with progress, allowing to see progress of the operation before completion.
* Interoperability between platforms through conversion (see below).
* Support for cyclic structures (for example, children linking to parents).

## Interoperability between platforms
Very important: the binary format used doesn't allow exchange between platforms with different endianness or pointer size. To obtain that, either use another serializer more suited to platform-agnostic data exchange, or use the text format of PolySerializer that let you read data for object serialized for one platform in a format independant of endianness. This operation is not optimized for performance, but it does the job.

## How to use
The code to serialize an object is very simple. The following example demonstrate serialization to a file:

  ```cs
  Serializer s = new Serializer();
  MyClass myObject = new MyClass();
    
  using (FileStream fs = new FileStream("myfile.bin", FileMode.Create, FileAccess.Write))
  {
    s.Serialize(fs, myObject);
  }
  ```

The code above created a file, `myfile.bin`. This file can be exchanged and deserialized as follow:

  ```cs
  MyClass myObject;
  using (FileStream fs = new FileStream("myfile.bin", FileMode.Open, FileAccess.Read))
  {
    myObject = (MyClass)s.Deserialize(fs);
  }
  ```

Several options allow you to control these operations.
### Modes
Serialization can operate in one of 3 modes. It is critical to understand that the mode is decided at serialization time, and cannot be changed thereafter without some work.

1. The default mode. This mode assumes that MyClass' interface did not change after serialization and before deserialization, and in particular that no field or property was added, removed or renamed. Reordering is allowed so this is really about interface compatibility.
2. The member name mode. Suited for archiving, this mode allows new fields or properties to be added, but does not allow removal or renaming. If an interface is extended with new members, old serialized files can still be deserialized, and new members will take default values.
3. The member order mode. In this mode, fields and properties are serialized in the order they are declared. In that case, renaming a member, or adding new members is supported, but obviously not reordering. It matches more closely the class implementation rather than the interface. 
### Formats
Serialization can output binary data, which is more efficient but opaque, or human-readable text. The later case is useful when performance isn't an issue, and it would be practical to work with text file. 
For example, for the purpose of searches, one can serialize an object to a text stream and perform a complex search on the text. It is even possible to perform modifications on the text and then deserialize it, to obtain a modified object.
### Progress
Serializing or deserializing large objects can take significant time. Fortunately, PolySerializer includes some support for this:

+ Asynchronous operations with SerializeAsync and DeserializeAsync.
+ The `Progress` property that gives some idea of how the current operation is performing.
While it could be possible to follow the progress of deserialization, since the size of the input stream can be known in most cases (such as file streams), this is inherently not possible during serialization.
Instead, `Progress` provides an estimate of the number of objects that have been handled vs the number of objects detected. Both numbers grow together, so this can sometimes give a wierd result, but at least it is consistent. In particular, if the object is a collection, `Progress` gives an excellent preview of the time it will take to completion.
### Polymorphism
One of the main features of PolySerializer is the ability to process only serializable parts of an object. This has interesting uses but may require some careful configuration.
For example, consider an application manipulating data visually. Each data class can have a serializable ancestor with the meaningful content, and descendants containing transient graphical information, such as the position on the screen. When a user wants to save their work in a file, only the serializable part will be saved. When reading the file later, deserialization will provide a fresh object with all unimportant values set to their default.
Another use of polymorphism features is object transformation. The serializer object, once created with `Serializer s = new Serializer();` can be tuned to perform automatic replacement of serialized types by others:

+ AssemblyOverrideTable will replace the assembly that was used during serialization with another. As long as the new assembly contains similar types (same name and interface), objects created will be using types different than the original.
+ NamespaceOverrideTable is similar, and allows changing from types of one namespace to another.
+ TypeOverrideTable directly replace one type with another if the two options above would ot work.
+ OverrideGenericArguments indicates if the type replacement mechanism should apply to generic arguments or not.
+ Inserters
They are objects used to convert between collection types. For example, you can convert from arrays to List<> and vice-versa. Custom inserters are used in the case of exotic collection types such as the one that was used to verify PolySerializer can handle collections of more than 5 billions items.
 
