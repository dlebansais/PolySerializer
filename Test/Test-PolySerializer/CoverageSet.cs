namespace Test
{
    using NUnit.Framework;
    using PolySerializer;
    using System;
    using System.Reflection;

    [TestFixture]
    public class CoverageSet
    {
        [Test]
        public static void TestCheckedObject()
        {
            string Text;

            CheckedObject NewObject = new CheckedObject(typeof(int), 0);
            NewObject.SetChecked();

            Text = NewObject.ToString();
        }

        [Test]
        public static void TestSerializedMember()
        {
            string Text;
            Type BasicType = typeof(string);
            MemberInfo[] MemberInfo = BasicType.GetMember("Clone");

            SerializedMember NewObject = new SerializedMember(MemberInfo[0]);

            NewObject.SetCondition(true);
            Text = NewObject.ToString();

            NewObject.SetCondition(false);
            Text = NewObject.ToString();
        }

        [Test]
        public static void TestDeserializedMember()
        {
            string Text;
            Type BasicType = typeof(string);
            MemberInfo[] MemberInfo = BasicType.GetMember("Clone");

            DeserializedMember NewObject = new DeserializedMember(MemberInfo[0]);
            NewObject.SetHasCondition();
            Text = NewObject.ToString();
        }

        [Test]
        public static void TestSerializableObject()
        {
            string Text;

            SerializableObject NewObject = new SerializableObject(string.Empty, typeof(string), 0);
            NewObject.SetSerialized();
            Text = NewObject.ToString();
        }

        [Test]
        public static void TestDeserializeObject()
        {
            string Text;

            DeserializedObject NewObject = new DeserializedObject(string.Empty, typeof(string), 0);
            NewObject.SetDeserialized();
            Text = NewObject.ToString();
        }

        [Test]
        public static void TestNamespaceDescriptor()
        {
            string Text;

            NamespaceDescriptor NewObject = new NamespaceDescriptor("test");
            Text = NewObject.ToString();
        }

        [Test]
        public static void TestTypeIdentifier()
        {
            TypeIdentifier Test0 = TypeIdentifier.Test0;
            TypeIdentifier Test1 = TypeIdentifier.Test1;
            TypeIdentifier Test2 = TypeIdentifier.Test2;
            TypeIdentifier Test3 = TypeIdentifier.Test3;
            TypeIdentifier Test4 = TypeIdentifier.Test4;
            TypeIdentifier Test5 = TypeIdentifier.Test5;
            TypeIdentifier Test6 = TypeIdentifier.Test6;
            TypeIdentifier Test7 = TypeIdentifier.Test7;
            TypeIdentifier Test8 = TypeIdentifier.Test8;
        }
    }
}
