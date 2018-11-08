using PolySerializer;
using System.Collections.Generic;
using System.IO;

namespace TestDebug
{
    [System.Serializable]
    public class ParentA
    {
        [PolySerializer.Serializable(Constructor= "Test0,Test1")]
        public ParentA(string s0, string s1)
        {
            Test2 = s1;
        }

        public bool IsAssigned;
        public string Test0 { get; set; }
        public string Test1 { get; set; }

        [PolySerializer.Serializable(Condition = "IsAssigned")]
        public string Test2 { get; set; }

        public sbyte  m0;
        public byte   m1;
        public short  m2;
        public ushort m3;
        public int    m4;
        public uint   m5;
        public long   m6;
        public ulong  m7;
        public float  m8;
        public double m9;
        public decimal m10;
        public System.Guid m11;
        public char m12;
        public List<string> m13;
        public ParentA m14;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Serializer s = new Serializer();
            s.FileFormat = SerializationFormat.TextPreferred;
            s.Mode = SerializationMode.MemberName;

            ParentA parentA0 = new ParentA("x", "test7");
            ParentA parentB0 = new ParentA("y", null);
            parentA0.IsAssigned = false;
            parentA0.Test0 = "test0";
            parentA0.Test1 = "test1";
            //parentA0.Test2 = "test2";
            parentA0.m0 = 0x70;
            parentA0.m1 = 0xD1;
            parentA0.m2 = 0x7071;
            parentA0.m3 = 0xD172;
            parentA0.m4 = 0x70717273;
            parentA0.m5 = 0xD0747576;
            parentA0.m6 = 0x7071727300000001;
            parentA0.m7 = 0xD071727300000002;
            parentA0.m8 = 1.23F;
            parentA0.m9 = 2.34;
            parentA0.m10 = 5.67m;
            parentA0.m11 = new System.Guid("{2BF50396-F2B4-4995-853A-9718C7192221}");
            parentA0.m12 = '$';
            parentA0.m13 = new List<string>() { "x", "y", "z" };
            parentA0.m14 = parentB0;
            parentB0.Test0 = "toto0";
            parentB0.m14 = parentB0;

            using (FileStream fs = new FileStream("test.log", FileMode.Create, FileAccess.Write))
            {
                s.Serialize(fs, parentA0);
            }

            ParentA parentA1;

            using (FileStream fs = new FileStream("test.log", FileMode.Open, FileAccess.Read))
            {
                parentA1 = s.Deserialize(fs) as ParentA;
            }
        }
    }
}
