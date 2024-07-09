namespace ishtar
{
    /// <summary>
    ///  RuntimeToken:
    ///     0xFFFF_____FFFF
    ///         |        |
    ///       ModuleID  EntityID
    /// </summary>
    public record struct RuntimeToken(ulong Value)
    {
        public static readonly RuntimeToken Default = new (0);

        public readonly uint ModuleID;
        public readonly uint ClassID;

        public RuntimeToken(uint moduleID, uint classID)
            : this(new cast_uint(moduleID, classID).Value)
        {
            ModuleID = moduleID;
            ClassID = classID;
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct cast_uint
        {
            [FieldOffset(0)]
            private readonly ulong _result;
            [FieldOffset(0)]
            private readonly uint _s1;
            [FieldOffset(4)]
            private readonly uint _s2;


            public cast_uint(uint s1, uint s2)
            {
                _result = 0;
                _s1 = s1;
                _s2 = s2;
            }
            // wtf resharper, _result is contains offset, why u are bully me?
            // ReSharper disable once ConvertToAutoProperty
            public ulong Value => _result;
        }
    }
}
