namespace ishtar.jit;

internal class _label_link
{
    internal _label_link()
    {
        offset = _constants.INVALID_ID;
        relocationID = _constants.INVALID_ID;
    }

    internal _label_link Previous { get; set; }

    internal int offset { get; set; }

    internal int displacement { get; set; }

    internal int relocationID { get; set; }
}
