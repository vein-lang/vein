namespace wave.emit
{
    public enum CallContext : byte
    {
        /// <summary>
        /// Call VM function.
        /// </summary>
        NATIVE_CALL,
        /// <summary>
        /// Call function with THIS context.
        /// </summary>
        THIS_CALL,
        /// <summary>
        /// Call static function.
        /// </summary>
        STATIC_CALL,
        /// <summary>
        /// Pop object from stack and call function with object context.
        /// </summary>
        BACKWARD_CALL
    };
}