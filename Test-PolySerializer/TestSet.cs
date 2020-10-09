namespace Test
{
    using PolySerializer;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.IO;
    using DeepEqual.Syntax;

    [TestFixture]
    public class TestSet
    {
        [OneTimeSetUp]
        public static void InitTestSession()
        {
            CultureInfo enUS = CultureInfo.CreateSpecificCulture("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUS;
            CultureInfo.DefaultThreadCurrentUICulture = enUS;
            Thread.CurrentThread.CurrentCulture = enUS;
            Thread.CurrentThread.CurrentUICulture = enUS;

            Assembly? PolySerializerAssembly;

            try
            {
                PolySerializerAssembly = Assembly.Load("PolySerializer");
            }
            catch
            {
                PolySerializerAssembly = null;
            }
            Assume.That(PolySerializerAssembly != null);
        }

        #region Basic Tests
        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic0(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ParentA parentA0 = new ParentA();
            parentA0.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentA0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check");
            }

            ParentA? parentA1;

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentA1 = s.Deserialize(fs) as ParentA;
            }

            Assert.That(parentA0.IsDeepEqual(parentA1), "Basic serializing");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic1(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ChildAA childAA0 = new ChildAA();
            childAA0.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, childAA0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of parent");
            }

            ChildAA? childAA1;

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                childAA1 = s.Deserialize(fs) as ChildAA;
            }

            Assert.That(childAA0.IsDeepEqual(childAA1), "Basic serializing of parent");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic2(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ChildAA childAA = new ChildAA();
            childAA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, childAA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(ChildAA), typeof(ChildAB) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic polymorphic check");
            }

            ChildAB? childAB;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                childAB = s.Deserialize(fs) as ChildAB;
            }

            Assert.That(childAA.IsDeepEqual(childAB), "Basic polymorphic serializing");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic3(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ChildAA childAA = new ChildAA();
            childAA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, childAA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(ChildAA), typeof(ParentA) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic polymorphic check child to parent");
            }

            ParentA? parentA;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentA = s.Deserialize(fs) as ParentA;
            }

            Assert.That(childAA.IsDeepEqual(parentA), "Basic polymorphic serializing child to parent");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic4(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ParentA parentA = new ParentA();
            parentA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(ParentA), typeof(ChildAA) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic polymorphic check parent to child");
            }

            ChildAA? childAA;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                childAA = s.Deserialize(fs) as ChildAA;
            }

            Assert.That(parentA.IsDeepEqual(childAA), "Basic polymorphic serializing parent to child");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic5(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            GrandChildAA grandChildAA0 = new GrandChildAA();
            grandChildAA0.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, grandChildAA0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic deep check of parent");
            }

            GrandChildAA? grandChildAA1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                grandChildAA1 = s.Deserialize(fs) as GrandChildAA;
            }

            Assert.That(grandChildAA0.IsDeepEqual(grandChildAA1), "Basic deep serializing of parent");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic6(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            GrandChildAA grandChildAA = new GrandChildAA();
            grandChildAA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, grandChildAA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(GrandChildAA), typeof(GrandChildAB) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic deep polymorphic check");
            }

            GrandChildAB? grandChildAB;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                grandChildAB = s.Deserialize(fs) as GrandChildAB;
            }

            Assert.That(grandChildAA.IsDeepEqual(grandChildAB), "Basic deep polymorphic serializing");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic7(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            GrandChildAA grandChildAA = new GrandChildAA();
            grandChildAA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, grandChildAA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(GrandChildAA), typeof(ParentA) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic deep polymorphic check child to parent");
            }

            ParentA? parentA;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentA = s.Deserialize(fs) as ParentA;
            }

            Assert.That(grandChildAA.IsDeepEqual(parentA), "Basic deep polymorphic serializing child to parent");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic8(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ParentA parentA = new ParentA();
            parentA.Test = "test";

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentA);
            }

            s.TypeOverrideTable = new Dictionary<Type, Type>() { { typeof(ParentA), typeof(GrandChildAA) } };

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic deep polymorphic check parent to child");
            }

            GrandChildAA? grandChildAA;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                grandChildAA = s.Deserialize(fs) as GrandChildAA;
            }

            Assert.That(parentA.IsDeepEqual(grandChildAA), "Basic deep polymorphic serializing parent to child");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic9(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ParentB parentB0 = new ParentB();
            parentB0.Init();

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentB0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of built-in types");
            }

            ParentB? parentB1;

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentB1 = s.Deserialize(fs) as ParentB;
            }

            parentB0.ShouldDeepEqual(parentB1);
            Assert.That(parentB0.IsDeepEqual(parentB1), "Basic serializing of built-in types");
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestBasic10(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            ParentC parentC0 = new ParentC();
            parentC0.InitInt(50);
            parentC0.InitString("60");
            parentC0.InitObject(new ParentA());

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentC0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of readonly properties (check should succeed)");
            }

            ParentC? parentC1;

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentC1 = s.Deserialize(fs) as ParentC;
            }

            Assert.That(!(parentC0.IsDeepEqual(parentC1)), "Basic serializing of readonly properties (should fail)");
        }
        #endregion

        #region Enum Tests
        public enum Enum0
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum0(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum0 test0 = Enum0.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type");
            }

            Enum0 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum0)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type");
        }

        public enum Enum1
        {
            test0 = 3,
            test1 = 4,
            test2 = 5,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum1(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum1 test0 = Enum1.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type with value");
            }

            Enum1 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum1)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type with value");
        }

        [Flags]
        public enum Enum2
        {
            test0 = 0x01,
            test1 = 0x02,
            test2 = 0x04,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum2(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum2 test0 = Enum2.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type with flag value");
            }

            Enum2 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum2)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type with flag value");
        }

        public enum Enum3 : byte
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum3(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum3 test0 = Enum3.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (byte)");
            }

            Enum3 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum3)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (byte)");
        }

        public enum Enum4 : sbyte
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum4(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum4 test0 = Enum4.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (sbyte)");
            }

            Enum4 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum4)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (sbyte)");
        }

        public enum Enum5 : short
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum5(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum5 test0 = Enum5.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (short)");
            }

            Enum5 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum5)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (short)");
        }

        public enum Enum6 : ushort
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum6(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum6 test0 = Enum6.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (ushort)");
            }

            Enum6 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum6)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (ushort)");
        }

        public enum Enum7 : int
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum7(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum7 test0 = Enum7.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (int)");
            }

            Enum7 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum7)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (int)");
        }

        public enum Enum8 : uint
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum8(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum8 test0 = Enum8.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (uint)");
            }

            Enum8 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum8)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (uint)");
        }

        public enum Enum10 : long
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum10(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum10 test0 = Enum10.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (long)");
            }

            Enum10 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum10)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (long)");
        }

        public enum Enum11 : ulong
        {
            test0,
            test1,
            test2,
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestEnum11(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Enum11 test0 = Enum11.test1;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of enum type (ulong)");
            }

            Enum11 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Enum11)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of enum type (ulong)");
        }
        #endregion

        #region Test Struct
        [System.Serializable]
        public struct Struct0
        {
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestStruct0(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Struct0 test0 = new Struct0();

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of empty struct");
            }

            Struct0 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Struct0)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of empty struct");
        }

        [System.Serializable]
        public struct Struct1
        {
            public bool field0;
            public byte field1;
            public sbyte field2;
            public char field3;
            public decimal field4;
            public double field5;
            public float field6;
            public int field7;
            public uint field8;
            public long field9;
            public ulong field10;
            public object field11;
            public short field12;
            public ushort field13;
            public string field14;
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public static void TestStruct1(int mode, int format)
        {
            Serializer s = new Serializer();
            s.Mode = (SerializationMode)mode;
            s.Format = (SerializationFormat)format;

            Struct1 test0 = new Struct1();

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, test0);
            }

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                Assert.That(s.Check(fs), "Basic check of empty struct");
            }

            Struct1 test1;
            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                test1 = (Struct1)s.Deserialize(fs);
            }

            Assert.That(test0.IsDeepEqual(test1), "Basic serializing of empty struct");
        }
        #endregion
    }
}
