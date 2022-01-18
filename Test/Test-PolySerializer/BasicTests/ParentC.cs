namespace Test;

using PolySerializer;

[Serializable]
public class ParentC
{
    public void InitInt(int Value)
    {
        PropInt = Value;
    }

    public void InitString(string Value)
    {
        PropString = Value;
    }

    public void InitObject(object Value)
    {
        PropObject = Value;
    }

    public int PropInt { get; private set; }
    public string? PropString { get; private set; }
    public object? PropObject { get; private set; }
}
