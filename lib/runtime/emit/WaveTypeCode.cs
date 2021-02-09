namespace wave.emit
{
    public enum WaveTypeCode
    {
        TYPE_NONE = 0x0,
        TYPE_VOID,
        TYPE_OBJECT,
        TYPE_BOOLEAN,
        TYPE_CHAR,
        TYPE_I1, /* sbyte  */
        TYPE_U1, /* byte   */
        TYPE_I2, /* short  */
        TYPE_U2, /* ushort */
        TYPE_I4, /* int32  */
        TYPE_U4, /* uint32 */
        TYPE_I8, /* long   */
        TYPE_U8, /* ulong  */
        TYPE_R4, /* float  */
        TYPE_R8, /* double */
        TYPE_R16, /* decimal */
        TYPE_STRING,
        TYPE_CLASS,
        TYPE_ARRAY
    }
}