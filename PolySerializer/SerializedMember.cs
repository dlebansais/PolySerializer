namespace PolySerializer
{
    using System.Reflection;

#pragma warning disable SA1600 // Elements should be documented
    internal class SerializedMember
    {
        #region Init
        public SerializedMember(MemberInfo memberInfo)
        {
            MemberInfo = memberInfo;
        }
        #endregion

        #region Properties
        public MemberInfo MemberInfo { get; private set; }
        public bool? Condition { get; private set; }
        #endregion

        #region Client Interface
        public void SetCondition(bool condition)
        {
            Condition = condition;
        }
        #endregion

        #region Debugging
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
        #endregion
    }
#pragma warning restore SA1600 // Elements should be documented
}
