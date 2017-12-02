using System.Reflection;

namespace PolySerializer
{
    public class SerializedMember
    {
        public SerializedMember(MemberInfo MemberInfo)
        {
            this.MemberInfo = MemberInfo;
        }

        public void SetCondition(bool ConditionValue)
        {
            Condition = ConditionValue;
        }

        public MemberInfo MemberInfo { get; private set; }
        public bool? Condition { get; private set; }

        public override string ToString()
        {
            string Result = MemberInfo.Name;

            if (Condition.HasValue)
                if (Condition.Value)
                    Result += " (Condition: True)";
                else
                    Result += " (Condition: False)";

            return Result;
        }
    }
}
