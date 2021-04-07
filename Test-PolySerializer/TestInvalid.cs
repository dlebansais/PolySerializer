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
    public class TestInvalid0
    {
        public int Test { get; set; }
    }

    [System.Serializable]
    public class TestInvalid1
    {
        public TestInvalid0 Test { get; set; } = new TestInvalid0();
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
        }
    }
}
