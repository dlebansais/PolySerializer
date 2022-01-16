namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
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
        [PolySerializer.Serializable(Constructor = "Test,Test")]
        public TestInvalid5(TestInvalid0 test0, TestInvalid0 test1)
        {
            Test = test1;
        }

        public TestInvalid0 Test { get; set; }
    }

    [System.Serializable]
    public class TestInvalid6
    {
        public decimal Test0 { get; set; }
        public double Test1 { get; set; }
        public float Test2 { get; set; }
        public string Test3 { get; set; } = string.Empty;
        public Guid Test4 { get; set; }
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

            Stream2.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream2);

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

            MemoryStream Stream6 = new MemoryStream();
            TestInvalid2 test6 = new TestInvalid2();
            test6.Test.Add(new TestInvalid0());

            s.Format = SerializationFormat.TextOnly;
            s.Mode = SerializationMode.Default;
            s.Serialize(Stream6, test6);

            Stream6.Seek(105, SeekOrigin.Begin);

            using (BinaryWriter Writer = new BinaryWriter(Stream6, Encoding.ASCII, true))
            {
                Writer.Write("xyz");
            }

            Stream6.Seek(0, SeekOrigin.Begin);
            IsCompatible = s.Check(Stream6);

            Assert.IsFalse(IsCompatible);
        }

        [Test]
        public static void InvalidConstructor()
        {
            Serializer s = new Serializer();
            bool IsCompatible;

            TestInvalid0 parameter = new TestInvalid0();
            TestInvalid5 test0 = new TestInvalid5(parameter, parameter);

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

        [Test]
        public static void BasicText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;
            bool IsCompatible;

            MemoryStream Stream0 = new MemoryStream();
            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);

            /*
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
            */
        }

        [Test]
        public static void InvalidConstructorText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;
            bool IsCompatible;

            string Text0 = "Mode=Default\n{Test.TestInvalid5, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null} !{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.xyz, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null} #0\n\n0x00000000{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null} #0\n";
            MemoryStream Stream0 = new MemoryStream(Encoding.ASCII.GetBytes(Text0));

            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);
        }

        [Test]
        public static void InvalidText()
        {
            bool IsCompatible;
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;

            string Text0 = "Mode=Default\r\n{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\r\n0x00000000";
            MemoryStream Stream0 = new MemoryStream(Encoding.ASCII.GetBytes(Text0));

            IsCompatible = s.Check(Stream0);

            Assert.IsFalse(IsCompatible);

            s.RootType = typeof(TestInvalid0);

            string Text1 = "Mode=xyz\n{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x00000000";
            MemoryStream Stream1 = new MemoryStream(Encoding.ASCII.GetBytes(Text1));

            Assert.Throws(typeof(InvalidDataException), () => s.Check(Stream1));

            Stream1.Seek(0, SeekOrigin.Begin);

            Assert.Throws(typeof(InvalidDataException), () => s.Deserialize(Stream1));

            string Text2 = "Mode=Default\r\n{Test.xyz, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x00000000";
            MemoryStream Stream2 = new MemoryStream(Encoding.ASCII.GetBytes(Text2));

            IsCompatible = s.Check(Stream2);

            Assert.IsFalse(IsCompatible);

            Stream2.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream2);

            string Text3 = "Mode=Default\n{Test.TestInvalid1, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n{Test.xyz, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x0000000a";
            MemoryStream Stream3 = new MemoryStream(Encoding.ASCII.GetBytes(Text3));

            IsCompatible = s.Check(Stream3);

            Assert.IsFalse(IsCompatible);

            string Text4 = "Mode=Default\n{Test.TestInvalid2, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n{System.Collections.Generic.List`1[[Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089} *2\n{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.xyz, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x000000040x000000000x00000000";
            MemoryStream Stream4 = new MemoryStream(Encoding.ASCII.GetBytes(Text4));

            IsCompatible = s.Check(Stream4);

            Assert.IsFalse(IsCompatible);

            string Text5 = "Mode=Default\n{Test.TestInvalid4, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n{Test.TestInvalid3, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}{Test.xyz, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x00000000";
            MemoryStream Stream5 = new MemoryStream(Encoding.ASCII.GetBytes(Text5));

            IsCompatible = s.Check(Stream5);

            Assert.IsFalse(IsCompatible);
        }

        [Test]
        public static void InvalidTextDigit()
        {
            bool IsCompatible;
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;

            string Text6 = "Mode=Default\n{Test.TestInvalid4, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n{Test.TestInvalid3, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x000000aX";
            MemoryStream Stream6 = new MemoryStream(Encoding.ASCII.GetBytes(Text6));

            IsCompatible = s.Check(Stream6);

            Assert.IsFalse(IsCompatible);

            Stream6.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream6);

            string Text5 = "Mode=Default\n{Test.TestInvalid2, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n{System.Collections.Generic.List`1[[Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089} *2X\n{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n;{Test.TestInvalid0, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n0x000000200x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x000000000x00000000";
            MemoryStream Stream5 = new MemoryStream(Encoding.ASCII.GetBytes(Text5));

            Stream5.Seek(0, SeekOrigin.Begin);
            s.Deserialize(Stream5);
        }

        [Test]
        public static void InvalidDataText()
        {
            Serializer s = new Serializer();
            s.Format = SerializationFormat.TextPreferred;

            /*
            MemoryStream Stream0 = new MemoryStream();
            TestInvalid6 test0 = new TestInvalid6();
            test0.Test0 = 1.01m;
            test0.Test1 = 1.0;
            test0.Test2 = 1.0f;
            test0.Test3 = "*";
            test0.Test4 = new Guid("{5FE2E93D-DA61-4AB7-8DE0-6E11ABFAD07B}");

            s.Serialize(Stream0, test0);
            Stream0.Seek(0, SeekOrigin.Begin);
            using (StreamReader sr = new StreamReader(Stream0))
            {
                string Text = sr.ReadToEnd();
            }
            */

            string Text1 = "Mode=Default\r\n{Test.TestInvalid6, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n1.X1m;Xd;Xf;X*\";{Xfe2e93d-da61-4ab7-8de0-6e11abfad07b}";

            MemoryStream Stream1 = new MemoryStream(Encoding.ASCII.GetBytes(Text1));
            s.Deserialize(Stream1);

            string Text2 = "Mode=Default\n{Test.TestInvalid6, Test-PolySerializer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null}\n1.01m;1d;1f;\"\\n\";{5fe2e93d-da61-4ab7-8de0-6e11abfad07b}";

            MemoryStream Stream2 = new MemoryStream(Encoding.ASCII.GetBytes(Text2));
            s.Deserialize(Stream2);
        }
    }
}
