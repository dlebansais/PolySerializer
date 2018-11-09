using System;

namespace PolySerializer
{
    #region Interface
    internal interface ICheckedObject
    {
        Type CheckedType { get; }
        long Count { get; }
        void SetChecked();
    }
    #endregion

    internal class CheckedObject : ICheckedObject
    {
        #region Init
        public CheckedObject(Type CheckedType, long Count)
        {
            this.CheckedType = CheckedType;
            this.Count = Count;
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
            string Result = CheckedType.FullName;

            if (IsChecked)
                Result += " (Checked)";

            return Result;
        }
        #endregion
    }
}
