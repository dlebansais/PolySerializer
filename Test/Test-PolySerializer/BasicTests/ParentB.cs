namespace Test
{
    using PolySerializer;

    [Serializable]
    public class ParentB
    {
        static readonly System.Guid TestGuid = new System.Guid("{4DA159C1-4B37-4DEE-A4FC-0F27F3FE9C15}");

        public void Init()
        {
            FieldBoolean = true;
            FieldByte = 0xAA;
            FieldSByte = -12;
            FieldChar0 = '@';
            FieldChar1 = '\'';
            FieldDecimal = 1.001m;
            FieldDouble = 1.01;
            FieldSingle = 1.1f;
            FieldInt32 = -32;
            FieldUInt32 = 0xFFFFFFFE;
            FieldInt64 = -64;
            FieldUInt64 = 0xFFFFFFFEFFFFFFFE;
            FieldObject = new ParentA();
            FieldInt16 = -16;
            FieldUInt16 = 0xFFFE;
            FieldString0 = "FieldString";
            FieldString1 = null;
            FieldGuid = TestGuid;

            PropBoolean = true;
            PropByte = 0xAA;
            PropSByte = -12;
            PropChar = '@';
            PropDecimal = 1.001m;
            PropDouble = 1.01;
            PropSingle = 1.1f;
            PropInt32 = -32;
            PropUInt32 = 0xFFFFFFFE;
            PropInt64 = -64;
            PropUInt64 = 0xFFFFFFFEFFFFFFFE;
            PropObject = new ParentA();
            PropInt16 = -16;
            PropUInt16 = 0xFFFE;
            PropString = "PropString";
            PropGuid = TestGuid;

            ProtFieldBoolean = true;
            ProtFieldByte = 0xAA;
            ProtFieldSByte = -12;
            ProtFieldChar = '@';
            ProtFieldDecimal = 1.001m;
            ProtFieldDouble = 1.01;
            ProtFieldSingle = 1.1f;
            ProtFieldInt32 = -32;
            ProtFieldUInt32 = 0xFFFFFFFE;
            ProtFieldInt64 = -64;
            ProtFieldUInt64 = 0xFFFFFFFEFFFFFFFE;
            ProtFieldObject = new ParentA();
            ProtFieldInt16 = -16;
            ProtFieldUInt16 = 0xFFFE;
            ProtFieldString = "ProtFieldString";
            ProtFieldGuid = TestGuid;

            ProtFPropBoolean = true;
            ProtFPropByte = 0xAA;
            ProtFPropSByte = -12;
            ProtFPropChar = '@';
            ProtFPropDecimal = 1.001m;
            ProtFPropDouble = 1.01;
            ProtFPropSingle = 1.1f;
            ProtFPropInt32 = -32;
            ProtFPropUInt32 = 0xFFFFFFFE;
            ProtFPropInt64 = -64;
            ProtFPropUInt64 = 0xFFFFFFFEFFFFFFFE;
            ProtFPropObject = new ParentA();
            ProtFPropInt16 = -16;
            ProtFPropUInt16 = 0xFFFE;
            ProtFPropString = "ProtFPropString";
            ProtFPropGuid = TestGuid;

            PrivFieldBoolean = true;
            PrivFieldByte = 0xAA;
            PrivFieldSByte = -12;
            PrivFieldChar = '@';
            PrivFieldDecimal = 1.001m;
            PrivFieldDouble = 1.01;
            PrivFieldSingle = 1.1f;
            PrivFieldInt32 = -32;
            PrivFieldUInt32 = 0xFFFFFFFE;
            PrivFieldInt64 = -64;
            PrivFieldUInt64 = 0xFFFFFFFEFFFFFFFE;
            PrivFieldObject = new ParentA();
            PrivFieldInt16 = -16;
            PrivFieldUInt16 = 0xFFFE;
            PrivFieldString = "PrivFieldString";
            PrivFieldGuid = TestGuid;

            PrivFPropBoolean = true;
            PrivFPropByte = 0xAA;
            PrivFPropSByte = -12;
            PrivFPropChar = '@';
            PrivFPropDecimal = 1.001m;
            PrivFPropDouble = 1.01;
            PrivFPropSingle = 1.1f;
            PrivFPropInt32 = -32;
            PrivFPropUInt32 = 0xFFFFFFFE;
            PrivFPropInt64 = -64;
            PrivFPropUInt64 = 0xFFFFFFFEFFFFFFFE;
            PrivFPropObject = new ParentA();
            PrivFPropInt16 = -16;
            PrivFPropUInt16 = 0xFFFE;
            PrivFPropString = "PrivFPropString";
            PrivFPropGuid = TestGuid;
        }

        public bool Use()
        {
            if (PrivFieldBoolean)
            if (PrivFieldByte == 0xAA)
            if (PrivFieldSByte == -12)
            if (PrivFieldChar == '@')
            if (PrivFieldDecimal == 1.001m)
            if (PrivFieldDouble == 1.01)
            if (PrivFieldSingle == 1.1f)
            if (PrivFieldInt32 == -32)
            if (PrivFieldUInt32 == 0xFFFFFFFE)
            if (PrivFieldInt64 == -64)
            if (PrivFieldUInt64 == 0xFFFFFFFEFFFFFFFE)
            if (PrivFieldObject == new ParentA())
            if (PrivFieldInt16 == -16)
            if (PrivFieldUInt16 == 0xFFFE)
            if (PrivFieldString == "PrivFieldString")
            if (PrivFieldGuid == TestGuid)
            return true;

            return false;
        }

        public bool FieldBoolean;
        public byte FieldByte;
        public sbyte FieldSByte;
        public char FieldChar0;
        public char FieldChar1;
        public decimal FieldDecimal;
        public double FieldDouble;
        public float FieldSingle;
        public int FieldInt32;
        public uint FieldUInt32;
        public long FieldInt64;
        public ulong FieldUInt64;
        public object? FieldObject;
        public short FieldInt16;
        public ushort FieldUInt16;
        public string? FieldString0;
        public string? FieldString1;
        public System.Guid FieldGuid;

        public bool PropBoolean { get; set; }
        public byte PropByte { get; set; }
        public sbyte PropSByte { get; set; }
        public char PropChar { get; set; }
        public decimal PropDecimal { get; set; }
        public double PropDouble { get; set; }
        public float PropSingle { get; set; }
        public int PropInt32 { get; set; }
        public uint PropUInt32 { get; set; }
        public long PropInt64 { get; set; }
        public ulong PropUInt64 { get; set; }
        public object? PropObject { get; set; }
        public short PropInt16 { get; set; }
        public ushort PropUInt16 { get; set; }
        public string? PropString { get; set; }
        public System.Guid PropGuid { get; set; }

        protected bool ProtFieldBoolean;
        protected byte ProtFieldByte;
        protected sbyte ProtFieldSByte;
        protected char ProtFieldChar;
        protected decimal ProtFieldDecimal;
        protected double ProtFieldDouble;
        protected float ProtFieldSingle;
        protected int ProtFieldInt32;
        protected uint ProtFieldUInt32;
        protected long ProtFieldInt64;
        protected ulong ProtFieldUInt64;
        protected object? ProtFieldObject;
        protected short ProtFieldInt16;
        protected ushort ProtFieldUInt16;
        protected string? ProtFieldString;
        protected System.Guid ProtFieldGuid;

        protected bool ProtFPropBoolean { get; set; }
        protected byte ProtFPropByte { get; set; }
        protected sbyte ProtFPropSByte { get; set; }
        protected char ProtFPropChar { get; set; }
        protected decimal ProtFPropDecimal { get; set; }
        protected double ProtFPropDouble { get; set; }
        protected float ProtFPropSingle { get; set; }
        protected int ProtFPropInt32 { get; set; }
        protected uint ProtFPropUInt32 { get; set; }
        protected long ProtFPropInt64 { get; set; }
        protected ulong ProtFPropUInt64 { get; set; }
        protected object? ProtFPropObject { get; set; }
        protected short ProtFPropInt16 { get; set; }
        protected ushort ProtFPropUInt16 { get; set; }
        protected string? ProtFPropString { get; set; }
        protected System.Guid ProtFPropGuid { get; set; }

        private bool PrivFieldBoolean;
        private byte PrivFieldByte;
        private sbyte PrivFieldSByte;
        private char PrivFieldChar;
        private decimal PrivFieldDecimal;
        private double PrivFieldDouble;
        private float PrivFieldSingle;
        private int PrivFieldInt32;
        private uint PrivFieldUInt32;
        private long PrivFieldInt64;
        private ulong PrivFieldUInt64;
        private object? PrivFieldObject;
        private short PrivFieldInt16;
        private ushort PrivFieldUInt16;
        private string? PrivFieldString;
        private System.Guid PrivFieldGuid;

        private bool PrivFPropBoolean { get; set; }
        private byte PrivFPropByte { get; set; }
        private sbyte PrivFPropSByte { get; set; }
        private char PrivFPropChar { get; set; }
        private decimal PrivFPropDecimal { get; set; }
        private double PrivFPropDouble { get; set; }
        private float PrivFPropSingle { get; set; }
        private int PrivFPropInt32 { get; set; }
        private uint PrivFPropUInt32 { get; set; }
        private long PrivFPropInt64 { get; set; }
        private ulong PrivFPropUInt64 { get; set; }
        private object? PrivFPropObject { get; set; }
        private short PrivFPropInt16 { get; set; }
        private ushort PrivFPropUInt16 { get; set; }
        private string? PrivFPropString { get; set; }
        private System.Guid PrivFPropGuid { get; set; }
    }
}
