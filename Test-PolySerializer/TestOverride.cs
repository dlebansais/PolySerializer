namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
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
        public int Test { get; set; }
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
    }
}
