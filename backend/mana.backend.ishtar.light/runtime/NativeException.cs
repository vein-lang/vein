namespace ishtar
{
    public class NativeException
    {
        public WNE code;
        public string msg;
        public CallFrame frame;
    };

    public enum WNE
    {
        NONE = 0,
        MISSING_METHOD,
        MISSING_FIELD,
        MISSING_TYPE,
        TYPE_LOAD,
        TYPE_MISMATCH,
        MEMBER_ACCESS,
        STATE_CORRUPT,
        ASSEMBLY_COULD_NOT_LOAD,
        // unexpected end of executable memory.
        END_EXECUTE_MEMORY,
        OUT_OF_MEMORY,
        ACCESS_VIOLATION,
        OVERFLOW
    }
}
