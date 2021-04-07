namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    [System.Serializable]
    class TestAttributes0
    {
        [PolySerializer.Serializable(Setter = "SetTest")]
        public int Test { get; private set; }

        public void SetTest(int n)
        {
            Test = n;
        }
    }

    [TestFixture]
    public class TestAttributes
    {
        [Test]
        public static void Basic()
        {
            Serializer s = new Serializer();
            s.RootType = typeof(TestAttributes0);

            TestAttributes0 test0 = new TestAttributes0();
            test0.SetTest(1);

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream);

            Assert.AreEqual(1, test0Copy.Test);
        }

        [Test]
        public static void Namespace()
        {
            Serializer s = new Serializer();
            s.RootType = typeof(TestAttributes0);

            Dictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable = new();
            s.NamespaceOverrideTable = NamespaceOverrideTable;

            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            AssemblyName CurrentAssemblyName = CurrentAssembly.GetName();
            byte[] PublicToken = CurrentAssemblyName.GetPublicKeyToken();

            NamespaceDescriptor Descriptor1 = new NamespaceDescriptor(nameof(Test));
            NamespaceDescriptor Descriptor2 = new NamespaceDescriptor(nameof(Test), CurrentAssemblyName.Name);
            NamespaceDescriptor Descriptor3 = new NamespaceDescriptor(nameof(Test), CurrentAssemblyName.Name, CurrentAssemblyName.Version.ToString());
            NamespaceDescriptor Descriptor4 = new NamespaceDescriptor(nameof(Test), CurrentAssembly.FullName, CurrentAssemblyName.Version.ToString(), CurrentAssemblyName.CultureName, PublicToken.ToString());
            NamespaceOverrideTable.Add(Descriptor1, Descriptor4);
            NamespaceOverrideTable.Add(Descriptor2, Descriptor4);
            NamespaceOverrideTable.Add(Descriptor3, Descriptor4);

            TestAttributes0 test0 = new TestAttributes0();
            test0.SetTest(1);

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream);

            Assert.AreEqual(1, test0Copy.Test);
        }
    }
}
