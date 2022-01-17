namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    [System.Serializable]
    public class TestAttributes0
    {
        [PolySerializer.Serializable(Setter = "SetTest1")]
        public int Test { get; private set; }

        public void SetTest0(int n, int p)
        {
            Test = n;
        }

        public void SetTest1(int n)
        {
            Test = n;
        }

        [PolySerializer.Serializable(Setter = "")]
        public int Test2 { get; private set; }
    }

    namespace Test1
    {
        [System.Serializable]
        public class TestAttributes1
        {
            public int Test { get; set; }
        }
    }

    [System.Serializable]
    public class TestAttributes2<T>
        where T : struct
    {
        public T Test { get; set; }
    }

    [System.Serializable]
    public class TestAttributes3
    {
        [PolySerializer.Serializable(Condition = "TestCondition")]
        public int Test { get; set; }

        public bool TestCondition { get; set; }

        [PolySerializer.Serializable(Condition = "TestCondition2")]
        public int Test2 { get; set; }

        public bool TestCondition2;
    }

    [System.Serializable]
    public class TestAttributes4
    {
        [PolySerializer.Serializable(Constructor = "")]
        public TestAttributes4()
        {
        }

        [PolySerializer.Serializable(Constructor = "Test,Test")]
        public TestAttributes4(int test0, int test, int test2)
        {
            Test = test0;
        }

        [PolySerializer.Serializable(Constructor = "SetTest,Test,Test,Test")]
        public TestAttributes4(int test0, int test1, int test2, int test3)
        {
            Test = test0;
        }

        [PolySerializer.Serializable(Constructor = "ClearTest,Test,Test,Test,Test")]
        public TestAttributes4(int test0, int test1, int test2, int test3, int test4)
        {
            Test = test0;
        }

        [PolySerializer.Serializable(Constructor = "Test,Test")]
        public TestAttributes4(int test0, int test1)
        {
            Test = test0;
        }

        public int Test { get; set; }

        public void SetTest()
        {
        }

        public void SetTest(int n)
        {
        }

        public void ClearTest()
        {
        }
    }

    [System.Serializable]
    public class TestAttributes5
    {
        public static int Test0;

        [PolySerializer.Serializable(Exclude = true)]
        public int Test1;
    }

    [System.Serializable]
    public class TestAttributes6
    {
        [PolySerializer.Serializable(Constructor = "Test,Test")]
        public TestAttributes6(TestAttributes5 test0, TestAttributes5 test1)
        {
            Test = test0;
        }

        public TestAttributes5 Test { get; set; }
    }

    [TestFixture]
    public class TestAttributes
    {
        [Test]
        public static void Basic()
        {
            Serializer s = new Serializer();

            TestAttributes0 test0 = new TestAttributes0();
            test0.SetTest1(1);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            s.RootType = null;

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream0);

            Assert.AreEqual(1, test0Copy.Test);

            MemoryStream Stream1 = new MemoryStream();
            Task SerializeTask = s.SerializeAsync(Stream1, test0);
            SerializeTask.Wait();

            Stream1.Seek(0, SeekOrigin.Begin);
            Task<object> DeserializeTask = s.DeserializeAsync(Stream1);
            DeserializeTask.Wait();

            TestAttributes0 test1Copy = (TestAttributes0)DeserializeTask.Result;

            Assert.AreEqual(1, test1Copy.Test);
        }

        [Test]
        public static void BasicText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;

            TestAttributes0 test0 = new TestAttributes0();
            test0.SetTest1(1);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            s.RootType = null;

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream0);

            Assert.AreEqual(1, test0Copy.Test);

            MemoryStream Stream1 = new MemoryStream();
            Task SerializeTask = s.SerializeAsync(Stream1, test0);
            SerializeTask.Wait();

            Stream1.Seek(0, SeekOrigin.Begin);
            Task<object> DeserializeTask = s.DeserializeAsync(Stream1);
            DeserializeTask.Wait();

            TestAttributes0 test1Copy = (TestAttributes0)DeserializeTask.Result;

            Assert.AreEqual(1, test1Copy.Test);
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

            Assert.NotNull(CurrentAssemblyName.Name);
            string Name = CurrentAssemblyName.Name!;

            Assert.NotNull(CurrentAssemblyName.Version);
            Version Version = CurrentAssemblyName.Version!;

            NamespaceDescriptor Descriptor1 = new NamespaceDescriptor(nameof(Test));
            NamespaceDescriptor Descriptor2 = new NamespaceDescriptor(nameof(Test), Name);
            NamespaceDescriptor Descriptor3 = new NamespaceDescriptor(nameof(Test), Name, Version.ToString());
            NamespaceDescriptor Descriptor4 = new NamespaceDescriptor(nameof(Test), Name, Version.ToString(), "neutral", "null");
            NamespaceDescriptor Descriptor5 = NamespaceDescriptor.DescriptorFromType(typeof(TestAttributes0));
            NamespaceDescriptor Descriptor6 = NamespaceDescriptor.DescriptorFromType(typeof(Test1.TestAttributes1));

            object Reference = Descriptor5;
            Assert.IsTrue(Descriptor4.Equals(Reference));
            Assert.IsTrue(Descriptor4.Equals(Descriptor5));
            Assert.IsTrue(Descriptor4 == Descriptor5);
            Assert.IsFalse(Descriptor4 != Descriptor5);

            NamespaceOverrideTable.Add(Descriptor1, Descriptor5);
            NamespaceOverrideTable.Add(Descriptor2, Descriptor5);
            NamespaceOverrideTable.Add(Descriptor3, Descriptor5);
            NamespaceOverrideTable.Add(Descriptor4, Descriptor5);

            TestAttributes0 test0 = new TestAttributes0();
            test0.SetTest1(1);

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream);

            Assert.AreEqual(1, test0Copy.Test);
        }

        [Test]
        public static void BadNamespace()
        {
            NamespaceDescriptor Descriptor0 = new NamespaceDescriptor(nameof(Test));
            NamespaceDescriptor Descriptor1 = new NamespaceDescriptor(nameof(Test));

            Assert.Throws(typeof(ArgumentException), () => NamespaceDescriptor.Match("*", Descriptor0, Descriptor1, out string NameOverride));
        }

        [Test]
        public static void NoNamespaceMatch()
        {
            Serializer s = new Serializer();
            s.RootType = typeof(TestAttributes0);

            Dictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable = new();
            s.NamespaceOverrideTable = NamespaceOverrideTable;

            NamespaceDescriptor Descriptor1 = new NamespaceDescriptor("unused");
            NamespaceDescriptor Descriptor2 = new NamespaceDescriptor("notused");
            NamespaceOverrideTable.Add(Descriptor1, Descriptor2);

            TestAttributes0 test0 = new TestAttributes0();

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            TestAttributes0 test0Copy = (TestAttributes0)s.Deserialize(Stream);

            Assert.AreEqual(0, test0Copy.Test);
        }

        [Test]
        public static void LongNamespace()
        {
            Serializer s = new Serializer();
            s.RootType = typeof(Test1.TestAttributes1);

            Dictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable = new();
            s.NamespaceOverrideTable = NamespaceOverrideTable;

            NamespaceDescriptor Descriptor1 = new NamespaceDescriptor("System.Windows");
            NamespaceDescriptor Descriptor2 = new NamespaceDescriptor("System.Microsoft");
            NamespaceOverrideTable.Add(Descriptor1, Descriptor2);

            Test1.TestAttributes1 test1 = new Test1.TestAttributes1();
            test1.Test = 1;

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test1);

            Stream.Seek(0, SeekOrigin.Begin);
            Test1.TestAttributes1 test1Copy = (Test1.TestAttributes1)s.Deserialize(Stream);

            Assert.AreEqual(1, test1Copy.Test);
        }

        [Test]
        public static void Generic()
        {
            Serializer s = new Serializer();

            Dictionary<NamespaceDescriptor, NamespaceDescriptor> NamespaceOverrideTable = new();
            s.NamespaceOverrideTable = NamespaceOverrideTable;

            NamespaceDescriptor Descriptor0 = NamespaceDescriptor.DescriptorFromType(typeof(TestAttributes2<int>));
            NamespaceDescriptor Descriptor1 = NamespaceDescriptor.DescriptorFromType(typeof(TestAttributes2<int>));
            NamespaceOverrideTable.Add(Descriptor0, Descriptor1);

            TestAttributes2<int> test2 = new TestAttributes2<int>();
            test2.Test = 2;

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test2);

            Stream.Seek(0, SeekOrigin.Begin);
            TestAttributes2<int> test2Copy = (TestAttributes2<int>)s.Deserialize(Stream);

            Assert.AreEqual(2, test2Copy.Test);
        }

        [Test]
        public static void CheckCondition()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestAttributes3 test0 = new TestAttributes3();
            test0.Test = 1;
            test0.TestCondition = false;

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsTrue(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes3 test0Copy = (TestAttributes3)s.Deserialize(Stream0);

            Assert.AreEqual(0, test0Copy.Test);

            TestAttributes3 test1 = new TestAttributes3();
            test1.Test = 1;
            test1.TestCondition = true;

            MemoryStream Stream1 = new MemoryStream();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream1);

            Assert.IsTrue(IsCompatible);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestAttributes3 test1Copy = (TestAttributes3)s.Deserialize(Stream1);

            Assert.AreEqual(1, test1Copy.Test);
        }

        [Test]
        public static void CheckConditionText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;
            bool IsCompatible;

            TestAttributes3 test0 = new TestAttributes3();
            test0.Test = 1;
            test0.TestCondition = false;

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsTrue(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes3 test0Copy = (TestAttributes3)s.Deserialize(Stream0);

            Assert.AreEqual(0, test0Copy.Test);

            TestAttributes3 test1 = new TestAttributes3();
            test1.Test = 1;
            test1.TestCondition = true;

            MemoryStream Stream1 = new MemoryStream();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream1);

            Assert.IsTrue(IsCompatible);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestAttributes3 test1Copy = (TestAttributes3)s.Deserialize(Stream1);

            Assert.AreEqual(1, test1Copy.Test);
        }

        [Test]
        public static void CheckConstructor()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestAttributes4 test0 = new TestAttributes4(1,1);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsTrue(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes4 test0Copy = (TestAttributes4)s.Deserialize(Stream0);

            Assert.AreEqual(1, test0Copy.Test);
        }

        [Test]
        public static void CheckConstructorWithInvalidItemBinary()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestAttributes5 testInit = new TestAttributes5();
            TestAttributes6 test0 = new TestAttributes6(testInit, testInit);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(406, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream0, Encoding.ASCII, true))
            {
                Writer.Write(new byte[] { 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB });
            }

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream0);
        }

        [Test]
        public static void CheckConstructorWithInvalidItemText()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestAttributes5 testInit = new TestAttributes5();
            TestAttributes6 test0 = new TestAttributes6(testInit, testInit);

            s.Format = SerializationFormat.TextOnly;
            s.Mode = SerializationMode.Default;

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(213, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream0, Encoding.ASCII, true))
            {
                Writer.Write(new byte[] { 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB, 0xDB });
            }

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream0);
        }

        [Test]
        public static void CheckConstructorText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;
            bool IsCompatible;

            TestAttributes4 test0 = new TestAttributes4(1,1);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsTrue(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes4 test0Copy = (TestAttributes4)s.Deserialize(Stream0);

            Assert.AreEqual(1, test0Copy.Test);
        }

        [Test]
        public static void NonSerializable()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestAttributes5 test0 = new TestAttributes5();

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsTrue(IsCompatible);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestAttributes5 test0Copy = (TestAttributes5)s.Deserialize(Stream0);
        }
    }
}
