namespace ishtar.jit;

internal class _label_data
{
    public _label_data(int contextId)
    {
        ctxID = contextId;
        offset = _constants.INVALID_ID;
    }

    public int offset { get; set; }

    public _label_link link { get; set; }

    public int ctxID { get; private set; }

    public _label_node ctxData { get; set; }
}
