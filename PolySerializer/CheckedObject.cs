namespace PolySerializer;

using System;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name
internal interface ICheckedObject
#pragma warning restore SA1649 // File name should match first type name
{
    Type CheckedType { get; }
    long Count { get; }
    void SetChecked();
}

internal class CheckedObject : ICheckedObject
{
    #region Init
    internal CheckedObject(Type checkedType, long count)
    {
        CheckedType = checkedType;
        Count = count;
        IsChecked = false;
    }
    #endregion

    #region Properties
    public Type CheckedType { get; private set; }
    public long Count { get; private set; }
    public bool IsChecked { get; private set; }
    #endregion

    #region Client Interface
    public void SetChecked()
    {
        IsChecked = true;
    }
    #endregion

    #region Debugging
    public override string ToString()
    {
        string Result = CheckedType.FullName !;

        if (IsChecked)
            Result += " (Checked)";

        return Result;
    }
    #endregion
}
#pragma warning restore SA1600 // Elements should be documented
