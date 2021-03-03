namespace wave.emit
{
    public enum CallContext : byte
    {
        NATIVE_CALL,
        THIS_CALL,
        STATIC_CALL,
        BACKWARD_CALL
    };
}