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
        OVERFLOW,
        OUT_OF_RANGE,
        NATIVE_LIBRARY_COULD_NOT_LOAD,
        NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND,
        MEMORY_LEAK,
        JIT_ASM_GENERATOR_TYPE_FAULT,
        JIT_ASM_GENERATOR_INCORRECT_CAST
    }
}
