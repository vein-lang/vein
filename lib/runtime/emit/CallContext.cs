namespace wave.emit
{
    public enum CallContext : byte
    {
        INTERNAL_CALL,
        SELF_CALL,
        OUTER_CALL
    };
}