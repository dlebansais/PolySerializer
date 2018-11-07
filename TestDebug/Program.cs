using PolySerializer;
using System.IO;

namespace TestDebug
{
    [System.Serializable]
    public class ParentA
    {
        public string Test;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Serializer s = new Serializer();
            s.FileFormat = SerializationFormat.TextPreferred;

            ParentA parentA0 = new ParentA();
            parentA0.Test = "test";

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
