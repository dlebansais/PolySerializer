namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;

    [System.Serializable]
    public class TestOverride0
    {
        public int Test { get; set; }
    }

    [System.Serializable]
    public class TestOverride1
    {
        public List<TestOverride0> Test { get; set; } = new List<TestOverride0>();
    }

    [System.Serializable]
    public class TestOverride2
    {
        public List<TestOverride0> Test { get; set; } = new List<TestOverride0>();
    }

    public class TestOverride3 : TestOverride2
    {
    }

    [TestFixture]
    public class TestOverride
    {
        [Test]
        public static void Basic()
        {
            Dictionary<Type, Type> TypeOverrideTable = new Dictionary<Type, Type>();
            TypeOverrideTable.Add(typeof(TestOverride1), typeof(TestOverride1));

            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            Dictionary<Assembly, Assembly> AssemblyOverrideTable = new Dictionary<Assembly, Assembly>();
            AssemblyOverrideTable.Add(CurrentAssembly, CurrentAssembly);

            Serializer s = new Serializer();
            s.TypeOverrideTable = TypeOverrideTable;
            s.AssemblyOverrideTable = AssemblyOverrideTable;

            MemoryStream Stream0 = new MemoryStream();
            TestOverride0 test0 = new TestOverride0();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0, SeekOrigin.Begin);
            TestOverride0 Test0Copy = (TestOverride0)s.Deserialize(Stream0);
        }

        [Test]
        public static void Generic()
        {
            Dictionary<Type, Type> TypeOverrideTable = new Dictionary<Type, Type>();
            TypeOverrideTable.Add(typeof(TestOverride0), typeof(TestOverride0));

            Serializer s = new Serializer();
            s.TypeOverrideTable = TypeOverrideTable;

            MemoryStream Stream1 = new MemoryStream();
            TestOverride1 test1 = new TestOverride1();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestOverride1 Test1Copy = (TestOverride1)s.Deserialize(Stream1);
        }

        [Test]
        public static void GenericByAssembly()
        {
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            Dictionary<Assembly, Assembly> AssemblyOverrideTable = new Dictionary<Assembly, Assembly>();
            AssemblyOverrideTable.Add(CurrentAssembly, CurrentAssembly);

            Serializer s = new Serializer();
            s.AssemblyOverrideTable = AssemblyOverrideTable;
            s.OverrideGenericArguments = false;

            MemoryStream Stream1 = new MemoryStream();
            TestOverride1 test1 = new TestOverride1();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestOverride1 Test1Copy = (TestOverride1)s.Deserialize(Stream1);
        }

        [Test]
        public static void OverrideByAssembly()
        {
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            Dictionary<Assembly, Assembly> AssemblyOverrideTable = new Dictionary<Assembly, Assembly>();
            AssemblyOverrideTable.Add(CurrentAssembly, CurrentAssembly);

            Serializer s = new Serializer();
            s.AssemblyOverrideTable = AssemblyOverrideTable;

            MemoryStream Stream1 = new MemoryStream();
            TestOverride1 test1 = new TestOverride1();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestOverride1 Test1Copy = (TestOverride1)s.Deserialize(Stream1);
        }

        [Test]
        public static void NoOverrideOfGeneric()
        {
            Dictionary<Type, Type> TypeOverrideTable = new Dictionary<Type, Type>();
            TypeOverrideTable.Add(typeof(TestOverride1), typeof(TestOverride1));

            Serializer s = new Serializer();
            s.TypeOverrideTable = TypeOverrideTable;

            MemoryStream Stream1 = new MemoryStream();
            TestOverride1 test1 = new TestOverride1();
            s.Serialize(Stream1, test1);

            Stream1.Seek(0, SeekOrigin.Begin);
            TestOverride1 Test1Copy = (TestOverride1)s.Deserialize(Stream1);
        }

        [Test]
        public static void ReadableCollections()
        {
            Serializer s = new Serializer();

            MemoryStream Stream3 = new MemoryStream();
            TestOverride3 test3 = new TestOverride3();
            s.Serialize(Stream3, test3);

            Stream3.Seek(0, SeekOrigin.Begin);
            TestOverride2 Test3Copy = (TestOverride2)s.Deserialize(Stream3);

            bool IsReadableCollection;

            IsReadableCollection = Serializer.IsReadableCollection(typeof(TestOverride2), Test3Copy, out IEnumerator Enumerator0);
            Assert.IsFalse(IsReadableCollection);

            List<TestOverride2> TestList1 = new List<TestOverride2>();
            IsReadableCollection = Serializer.IsReadableCollection(typeof(List<TestOverride2>), TestList1, out IEnumerator Enumerator1);
            Assert.IsTrue(IsReadableCollection);

            ExtraList<TestOverride3> TestList2 = new ExtraList<TestOverride3>();
            IsReadableCollection = Serializer.IsReadableCollection(typeof(ExtraList<TestOverride3>), TestList2, out IEnumerator Enumerator2);
            Assert.IsTrue(IsReadableCollection);
        }

        [Test]
        public static void WriteableCollections()
        {
            Serializer s = new Serializer();

            List<IInserter> CustomInserters = new List<IInserter>();
            CustomInserters.Add(s.BuiltInInserters[0]);
            CustomInserters.Add(s.BuiltInInserters[1]);
            CustomInserters.Add(s.BuiltInInserters[2]);
            s.CustomInserters = CustomInserters;

            MemoryStream Stream3 = new MemoryStream();
            TestOverride3 test3 = new TestOverride3();
            s.Serialize(Stream3, test3);

            Stream3.Seek(0, SeekOrigin.Begin);
            TestOverride2 Test3Copy = (TestOverride2)s.Deserialize(Stream3);

            bool IsWriteableCollection;

            IsWriteableCollection = s.IsWriteableCollection(Test3Copy, typeof(TestOverride2), out IInserter Inserter0, out Type ItemType0);
            Assert.IsFalse(IsWriteableCollection);

            IsWriteableCollection = s.IsWriteableCollection(Test3Copy, typeof(List<TestOverride2>), out IInserter Inserter1, out Type ItemType1);
            Assert.IsTrue(IsWriteableCollection);

            IsWriteableCollection = s.IsWriteableCollection(typeof(TestOverride2), out IInserter Inserter2, out Type ItemType2);
            Assert.IsFalse(IsWriteableCollection);

            IsWriteableCollection = s.IsWriteableCollection(typeof(List<TestOverride2>), out IInserter Inserter3, out Type ItemType3);
            Assert.IsTrue(IsWriteableCollection);
        }
    }
}
