namespace ishtar.jit;

using registers;

public class _label_node : _label
{
    internal _label_node(int id) : base(id)
    {
    }

    //public _label_node(int labelId) : base(CodeNodeType.Label) => LabelId = labelId;

    public int LabelId { get; private set; }

    public int ReferenceCount { get; set; }

    //public JumpNode From { get; set; }

    //public override string ToString()
    //    => string.Format("[{0}] {1}: Id={2}, From=({3})", FlowId == 0 ? "#" : FlowId.ToString(), Type, LabelId, From);
}
