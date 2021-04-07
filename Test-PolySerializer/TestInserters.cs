namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    [System.Serializable]
    public class TestInserters0
    {
        public int[] TestArray { get; set; } = new int[0];
        public List<int> TestList { get; set; } = new List<int>();
        public SortedSet<int> TestSet { get; set; } = new SortedSet<int>();
        public Dictionary<int, int> TestDictionary { get; set; } = new Dictionary<int, int>();
        public StringCollection TestStrings { get; set; } = new StringCollection();
        public TestInserters0? Self { get; set; }
    }

    [System.Serializable]
    public class TestInserters1
    {
        public string TestString { get; set; } = string.Empty;
    }

    [TestFixture]
    public class TestInserters
    {
        [Test]
        public static void Basic()
        {
            Serializer s = new Serializer();

            Assert.AreEqual(3, s.BuiltInInserters.Count);
            Assert.AreEqual("For arrays of any type (declared with the [] syntax)", s.BuiltInInserters[0].Description);
            Assert.AreEqual("For collections supporting both the IList and IList<T> interface", s.BuiltInInserters[1].Description);
            Assert.AreEqual("For generic types with an Add() method", s.BuiltInInserters[2].Description);

            TestInserters0 test0 = new TestInserters0();
            test0.TestArray = new int[1];
            test0.TestArray[0] = 1;
            test0.TestList = new List<int>();
            test0.TestList.Add(2);
            test0.TestSet = new SortedSet<int>();
            test0.TestSet.Add(3);
            test0.TestStrings.Add("*");
            test0.Self = test0;

            MemoryStream Stream = new MemoryStream();
            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            bool IsCompatible = s.Check(Stream);
            Assert.IsTrue(IsCompatible);

            Stream.Seek(0, SeekOrigin.Begin);
            Task<bool> IsCompatibleTask = s.CheckAsync(Stream);
            IsCompatibleTask.Wait();
            Assert.IsTrue(IsCompatibleTask.Result);

            Stream.Seek(0, SeekOrigin.Begin);
            TestInserters0 test0Copy = (TestInserters0)s.Deserialize(Stream);

            Assert.AreEqual(1, test0Copy.TestArray.Length);
            Assert.AreEqual(1, test0Copy.TestArray[0]);
            Assert.AreEqual(1, test0Copy.TestList.Count);
            Assert.AreEqual(2, test0Copy.TestList[0]);
            Assert.AreEqual(1, test0Copy.TestSet.Count);
            Assert.AreEqual(3, test0Copy.TestSet.Min);
            Assert.AreEqual(3, test0Copy.TestSet.Max);
            Assert.AreEqual(0, test0Copy.TestStrings.Count);
        }

        [Test]
        public static void BasicText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextOnly;

            Assert.AreEqual(3, s.BuiltInInserters.Count);
            Assert.AreEqual("For arrays of any type (declared with the [] syntax)", s.BuiltInInserters[0].Description);
            Assert.AreEqual("For collections supporting both the IList and IList<T> interface", s.BuiltInInserters[1].Description);
            Assert.AreEqual("For generic types with an Add() method", s.BuiltInInserters[2].Description);

            TestInserters0 test0 = new TestInserters0();
            test0.TestArray = new int[1];
            test0.TestArray[0] = 1;
            test0.TestList = new List<int>();
            test0.TestList.Add(2);
            test0.TestList.Add(4);
            test0.TestSet = new SortedSet<int>();
            test0.TestSet.Add(3);
            test0.TestStrings.Add("*");
            test0.Self = test0;

            MemoryStream Stream = new MemoryStream();
            using (StreamWriter Writer = new StreamWriter(Stream, Encoding.UTF8, 128, true))
            {
                Writer.Write("");
            }

            s.Serialize(Stream, test0);

            Stream.Seek(0, SeekOrigin.Begin);
            bool IsCompatible = s.Check(Stream);
            Assert.IsTrue(IsCompatible);

            Stream.Seek(0, SeekOrigin.Begin);
            Task<bool> IsCompatibleTask = s.CheckAsync(Stream);
            IsCompatibleTask.Wait();
            Assert.IsTrue(IsCompatibleTask.Result);

            Stream.Seek(0, SeekOrigin.Begin);
            TestInserters0 test0Copy = (TestInserters0)s.Deserialize(Stream);

            Assert.AreEqual(1, test0Copy.TestArray.Length);
            Assert.AreEqual(1, test0Copy.TestArray[0]);
            Assert.AreEqual(2, test0Copy.TestList.Count);
            Assert.AreEqual(2, test0Copy.TestList[0]);
            Assert.AreEqual(4, test0Copy.TestList[1]);
            Assert.AreEqual(1, test0Copy.TestSet.Count);
            Assert.AreEqual(3, test0Copy.TestSet.Min);
            Assert.AreEqual(3, test0Copy.TestSet.Max);
            Assert.AreEqual(0, test0Copy.TestStrings.Count);
        }

        [Test]
        public static void BigObject()
        {
            Serializer s = new Serializer();

            TestInserters1 test1 = new TestInserters1();

            string TestString = string.Empty;

            for (int i = 0; i < 10000; i++)
                TestString += "0123456789";

            test1.TestString = TestString;

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test1);
        }
    }
}
