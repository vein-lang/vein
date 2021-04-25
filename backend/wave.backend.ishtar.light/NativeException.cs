namespace ishtar
{
    public class NativeException
    {
        public WaveNativeException code;
        public string msg;
        public CallFrame frame;
    };

    public enum WaveNativeException
    {
        NONE = 0,
        MISSING_METHOD,
        MISSING_FIELD,
        TYPE_LOAD,
        MEMBER_ACCESS,
        STATE_CORRUPT,
        ASSEMBLY_COULD_NOT_LOAD,
        // unexpected end of executable memory.
        END_EXECUTE_MEMORY
    }
}