namespace ishtar.jit;

using registers;

public class _asm
{
    internal const uint MaxLookAhead = 64;
    private static int _idGenerator;
    private int _id;

    internal _ptr baseAddr { get; set; }

    private _code_buf _codeBuffer;
    private _code_ctx _codeContext;

    private opcode_opt _opcodeOpt;

    private List<_data_block> _data = new List<_data_block>();
    private List<_label_data> _labels = new List<_label_data>();

    internal _asm()
    {
        _codeBuffer = new _code_buf(this);
        _codeContext = new _code_ctx(this);

        ZDI = new _gp(_cpu.registers.RDI);
        ZSI = new _gp(_cpu.registers.RSI);
        ZBP = new _gp(_cpu.registers.RBP);
        ZSP = new _gp(_cpu.registers.RSP);
        ZBX = new _gp(_cpu.registers.RBX);
        ZDX = new _gp(_cpu.registers.RDX);
        ZCX = new _gp(_cpu.registers.RCX);
        ZAX = new _gp(_cpu.registers.RAX);

        _id = _idGenerator++;
    }


    internal _gp ZAX { get; private set; }

    internal _gp ZCX { get; private set; }

    internal _gp ZDX { get; private set; }

    internal _gp ZBX { get; private set; }

    internal _gp ZSP { get; private set; }

    internal _gp ZBP { get; private set; }

    internal _gp ZSI { get; private set; }

    internal _gp ZDI { get; private set; }


    public static _code_ctx<T> CreateContext<T>()
    {
        var t = typeof(T);

        var args = new Type[0];
        Type delType = null;
        if (t == typeof(Action))
        {
            //delType = DelegateCreator.NewDelegateType(args);
        }
        else if (_utils.Actions.Contains(t.GetGenericTypeDefinition()))
        {
            var gargs = t.GetGenericArguments();
            args = new Type[gargs.Length].init_with(i => gargs[i]);
            //delType = DelegateCreator.NewDelegateType(args);
        }
        else if (_utils.Funcs.Contains(t.GetGenericTypeDefinition()))
        {
            var gargs = t.GetGenericArguments();
            args = new Type[gargs.Length - 1].init_with(i => gargs[i]);
            var ret = gargs.Last();
            //delType = DelegateCreator.NewDelegateType(ret, args);
        }
        else
            throw new ArgumentException();
        var asm = new _asm();
        var ctx = new _code_ctx<T>(asm, delType);
        asm._codeContext = ctx;
        return ctx;
    }

    internal unsafe _ptr bake()
    {
        foreach (var dataItem in _data)
        {
            if (dataItem.Label == null)
            {
                throw new ArgumentException();
            }
            align(ALIGNING_MODE.DATA, dataItem.Alignment);
            bind(dataItem.Label.ID);
            foreach (var v in dataItem.Entities)
            {
                fixed (byte* pv = v.bytes)
                    embed(pv, v.bytes.Length);
            }
        }
        return _codeBuffer.bake();
    }

    internal _ptr bake(out int codeSize)
        => _codeBuffer.bake(out codeSize);

    public void data(_label label, int alignment, params _data_entity[] data)
        => _data.Add(new _data_block(label, alignment, data));

    internal void embed(_ptr data, int size)
        => _codeBuffer.embed(data, size);
    internal void align(ALIGNING_MODE alignMode, int offset)
        => _codeBuffer.align(alignMode, offset);

    internal void bind(int labelId) => _codeBuffer.bind(labelId);

    internal _label createLabel()
    {
        int id;
        CreateLabelData(out id);
        return new _label(id);
    }
    internal _label_data CreateLabelData(out int id)
    {
        var data = new _label_data(_id);
        id = _labels.Count;
        _labels.Add(data);
        return data;
    }


    #region emit

    internal void emit(_opcode instructionId)
        => _codeBuffer.emit(instructionId, _operand.INVALID, _operand.INVALID, _operand.INVALID, _operand.INVALID, _opcodeOpt);

    internal void emit(_opcode instructionId, _operand o0)
    {
        o0 = o0 ?? _operand.INVALID;
        _codeBuffer.Emit(instructionId, o0, _operand.INVALID, _operand.INVALID, _operand.INVALID, _opcodeOpt);
    }

    internal void emit(_opcode instructionId, _operand o0, _operand o1)
    {
        o0 = o0 ?? _operand.INVALID;
        o1 = o1 ?? _operand.INVALID;
        _codeBuffer.emit(instructionId, o0, o1, _operand.INVALID, _operand.INVALID, _opcodeOpt);
    }

    internal void emit(_opcode instructionId, _operand o0, _operand o1, _operand o2)
    {
        o0 = o0 ?? _operand.INVALID;
        o1 = o1 ?? _operand.INVALID;
        o2 = o2 ?? _operand.INVALID;
        _codeBuffer.emit(instructionId, o0, o1, o2, _operand.INVALID, _opcodeOpt);
    }

    internal void emit(_opcode instructionId, _operand o0, _operand o1, _operand o2, _operand o3)
    {
        o0 = o0 ?? _operand.INVALID;
        o1 = o1 ?? _operand.INVALID;
        o2 = o2 ?? _operand.INVALID;
        o3 = o3 ?? _operand.INVALID;
        _codeBuffer.emit(instructionId, o0, o1, o2, o3, _opcodeOpt);
    }

    internal void emit(_opcode instructionId, _operand o0, _operand o1, _operand o2, _operand o3, opcode_opt options)
    {
        _opcodeOpt = options;
        emit(instructionId, o0, o1, o2, o3);
    }

    #endregion
}

public class _data_block
{
    public _data_block(_label label, int alignment, params _data_entity[] entities)
    {
        Label = label;
        Entities = entities;
        Alignment = alignment;
    }

    public _data_entity[] Entities { get; private set; }

    public int Alignment { get; private set; }
		
    public _label Label { get; private set; }
}

public class _data_entity
{
    internal byte[] bytes { get; private set; }
    public static _data_entity Of(params byte[] v)
    {
        var d = new _data_entity {bytes = new byte[v.Length*sizeof(byte)]};
        Buffer.BlockCopy(v, 0, d.bytes, 0, d.bytes.Length);
        return d;
    }

    public static _data_entity Of(params sbyte[] v)
    {
        var d = new _data_entity { bytes = new byte[v.Length * sizeof(sbyte)] };
        Buffer.BlockCopy(v, 0, d.bytes, 0, d.bytes.Length);
        return d;
    }
}

public class _code_ctx<T> : _code_ctx
{
    public _code_ctx(_asm asm, Type t) : base(asm)
        => _delegateType = t;

    private Type _delegateType;

    public T Compile()
    {
        var fp = assembler.bake();
        return fp.ToCallable<T>(_delegateType);
    }

    public T Compile(out IntPtr raw, out int codeSize)
    {
        var fp = assembler.bake(out codeSize);
        raw = fp;
        return fp.ToCallable<T>(_delegateType);
    }
}
public class _code_ctx
{
    protected _asm assembler;

    public _code_ctx(_asm asm)
        => assembler = asm;

    public _label Label() => assembler.createLabel();

    public void bind(_label label) => assembler.bind(label.ID);

    public void align(ALIGNING_MODE mode, int size) => assembler.align(mode, size);

    public void data(_label label, params _data_entity[] entities) => data(label, 16, entities);

    public void data(_label label, int alignment, params _data_entity[] entities)
        => assembler.data(label, alignment, entities);
}



