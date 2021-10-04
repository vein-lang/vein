namespace ishtar
{
    using System.Runtime.InteropServices;

    /// <summary>
    ///  RuntimeToken:
    ///     0xFFFF_____FFFF
    ///         |        |
    ///       ModuleID  EntityID
    /// </summary>
    public record RuntimeToken(uint Value)
    {
        public static readonly RuntimeToken Default = new (0);

        public ushort ModuleID { get; }
        public ushort ClassID { get; }

        public RuntimeToken(ushort moduleID, ushort classID)
            : this(new cast_uint(moduleID, classID).Value)
        {
            ModuleID = moduleID;
            ClassID = classID;
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct cast_uint
        {
            [FieldOffset(0)]
            private readonly uint _result;
            [FieldOffset(0)]
            private readonly ushort _s1;
            [FieldOffset(2)]
            private readonly ushort _s2;


            public cast_uint(ushort s1, ushort s2)
            {
                _result = 0;
                _s1 = s1;
                _s2 = s2;
            }
            // wtf resharper, _result is contains offset, why u are bully me?
            // ReSharper disable once ConvertToAutoProperty
            public uint Value => _result;
        }
    }
}
