using System.Reflection;

namespace PolySerializer
{
    public class DeserializedMember
    {
        public DeserializedMember(MemberInfo MemberInfo)
        {
            this.MemberInfo = MemberInfo;
        }

        public void SetHasCondition()
        {
            HasCondition = true;
        }

        public void SetPropertySetter(MethodInfo PropertySetter)
        {
            this.PropertySetter = PropertySetter;
        }

        public MemberInfo MemberInfo { get; private set; }
        public bool HasCondition { get; private set; }
        public MethodInfo PropertySetter { get; private set; }

        public override string ToString()
        {
            string Result = MemberInfo.Name;

            if (HasCondition)
                Result += " (Has condition)";

            return Result;
        }
    }
}
