namespace PolySerializer;

using System.Reflection;

#pragma warning disable SA1600 // Elements should be documented
internal class DeserializedMember
{
    #region Init
    public DeserializedMember(MemberInfo memberInfo)
    {
        MemberInfo = memberInfo;
    }
    #endregion

    #region Properties
    public MemberInfo MemberInfo { get; private set; }
    public bool HasCondition { get; private set; }
    public MethodInfo? PropertySetter { get; private set; }
    #endregion

    #region Client Interface
    public void SetHasCondition()
    {
        HasCondition = true;
    }

    public void SetPropertySetter(MethodInfo propertySetter)
    {
        PropertySetter = propertySetter;
    }
    #endregion

    #region Debugging
    public override string ToString()
    {
        string Result = MemberInfo.Name;

        if (HasCondition)
            Result += " (Has condition)";

        return Result;
    }
    #endregion
}
#pragma warning restore SA1600 // Elements should be documented
