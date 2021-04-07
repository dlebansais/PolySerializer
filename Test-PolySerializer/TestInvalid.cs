namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    [System.Serializable]
    public class TestInvalid0
    {
        public int Test { get; set; }
    }

    [System.Serializable]
    public class TestInvalid1
    {
        public TestInvalid0 Test { get; set; } = new TestInvalid0();
    }

    [System.Serializable]
    public class TestInvalid2
    {
        public List<TestInvalid0> Test { get; set; } = new List<TestInvalid0>();
    }

    [System.Serializable]
    public struct TestInvalid3
    {
        public TestInvalid0 Test;
    }

    [System.Serializable]
    public class TestInvalid4
    {
        public TestInvalid3 Test { get; set; } = new TestInvalid3();
    }

    [System.Serializable]
    public class TestInvalid5
    {
        [PolySerializer.Serializable(Constructor = "Test")]
        public TestInvalid5(TestInvalid0 test)
        {
            Test = test;
        }

        public TestInvalid0 Test { get; set; }
    }

    [TestFixture]
    public class TestInvalid
    {
        [Test]
        public static void Basic()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            MemoryStream Stream0 = new MemoryStream();
            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);

            MemoryStream Stream1 = new MemoryStream();
            using (BinaryWriter Writer = new BinaryWriter(Stream1, Encoding.ASCII, true))
            {
                Writer.Write(0);
                Writer.Write(0);
                Writer.Write(0);
            }

            Stream1.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream1);

            Assert.IsFalse(IsCompatible);

            TestInvalid1 test2 = new TestInvalid1();
            
            MemoryStream Stream2 = new MemoryStream();
            s.Serialize(Stream2, test2);

            Stream2.Seek(0xDA, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream2, Encoding.ASCII, true))
            {
                Writer.Write("xyz");
            }

            Stream2.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream2);

            Assert.IsFalse(IsCompatible);

            MemoryStream Stream3 = new MemoryStream();
            using (BinaryWriter Writer = new BinaryWriter(Stream3, Encoding.ASCII, true))
            {
                Writer.Write(0xFFFFFFFF);
                Writer.Write(0xFFFFFFFF);
            }

            Stream3.Seek(0, SeekOrigin.Begin);

            s.RootType = null;

            IsCompatible = s.Check(Stream3);

            Assert.IsFalse(IsCompatible);

            MemoryStream Stream4 = new MemoryStream();
            TestInvalid2 test4 = new TestInvalid2();
            test4.Test.Add(new TestInvalid0());

            s.Serialize(Stream4, test4);

            Stream4.Seek(0x290, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream4, Encoding.ASCII, true))
            {
                Writer.Write("xyz");
            }

            Stream4.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream4);

            Assert.IsFalse(IsCompatible);

            TestInvalid4 test5 = new TestInvalid4();
            TestInvalid3 test51 = new TestInvalid3();
            test51.Test = new TestInvalid0();
            test5.Test = test51;

            MemoryStream Stream5 = new MemoryStream();
            s.Serialize(Stream5, test5);

            Stream5.Seek(0x191, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream5, Encoding.ASCII, true))
            {
                Writer.Write("xyz");
            }

            Stream5.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream5);

            Assert.IsFalse(IsCompatible);
        }

        [Test]
        public static void InvalidConstructor()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestInvalid0 parameter = new TestInvalid0();
            TestInvalid5 test0 = new TestInvalid5(parameter);

            MemoryStream Stream0 = new MemoryStream();
            s.Serialize(Stream0, test0);

            Stream0.Seek(0xDA, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream0, Encoding.ASCII, true))
            {
                Writer.Write("xyz");
            }

            Stream0.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);
        }
    }
}
