namespace ishtar.jit;

using ishtar.jit.registers;
using System;
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0044 // Add readonly modifier

internal class _code_buf
{
    public _operand Operand0 { get; private set; }
    public _operand Operand1 { get; private set; }
    public _operand Operand2 { get; private set; }
    public _operand Operand3 { get; set; }
    public long OpCode { get; set; }
    public long SecondaryOpCode { get; private set; }
    public long ImmediateValue { get; set; }
    public int ImmediateLength { get; set; }
    public int Base { get; set; }
    public int Index { get; set; }
    public _label_data Label { get; set; }
    public int DisplacementOffset { get; set; }
    public int DisplacementSize { get; set; }
    public int RelocationId { get; set; }
    public int ModRmRegister { get; set; }
    //public Memory ModRmMemory { get; set; }
    public long _operand { get; set; }
    public opcode_opt OpcodeOptions { get; set; }


    private enum RelocationMode
	{
		AbsToAbs = 0,
		RelToAbs = 1,
		AbsToRel = 2,
		Trampoline = 3
	}

	private sealed class RelocationData
	{
		public _ptr Data { get; set; }
		public _ptr From { get; set; }
		public int Size { get; set; }
		public RelocationMode Mode { get; set; }
	}

	private class OpCodeMm
	{
		public OpCodeMm(int length, byte[] data)
		{
			Length = length;
			Data = data;
		}

		public int Length { get; private set; }
		public byte[] Data { get; private set; }
	}

	private static OpCodeMm[] _opCodeMm =
	{
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(1, new byte[] {0x0F, 0x00, 0}),
		new OpCodeMm(2, new byte[] {0x0F, 0x38, 0}),
		new OpCodeMm(2, new byte[] {0x0F, 0x3A, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x00, 0x00, 0}),
		new OpCodeMm(0, new byte[] {0x0F, 0x01, 0})
	};

	private static byte[] _opCodePp =
	{
		0x00,
		0x66,
		0xF3,
		0xF2,
		0x00,
		0x00,
		0x00,
		0x9B
	};

	//private static _reg[] _patchedHiRegs =
	//{
	//	Compiler.GpbHi(4),
	//	Compiler.GpbHi(5),
	//	Compiler.GpbHi(6),
	//	Compiler.GpbHi(7)
	//};

	private static byte[] _segmentPrefix = { 0x00, 0x26, 0x2E, 0x36, 0x3E, 0x64, 0x65 };
	private static byte[] _opCodePushSeg = { 0x00, 0x06, 0x0E, 0x16, 0x1E, 0xA0, 0xA8 };
	private static byte[] _opCodePopSeg = { 0x00, 0x07, 0x00, 0x17, 0x1F, 0xA1, 0xA9 };

    //private EmitContextData _eh = new EmitContextData();

    private _asm _assembler;
    private _ptr _buffer;
	private _ptr _end;
	private _ptr _cursor;

    internal _ptr bake() => throw new NotImplementedException();

    private int _trampolinesSize;
	private List<RelocationData> _relocations = new List<RelocationData>();

    internal _ptr bake(out int codeSize) => throw new NotImplementedException();

    private _label_link _unusedLinks;


    public _code_buf(_asm asm)
    {
        
    }

    internal void emit(_opcode instructionId, _operand o0, _operand o1, _operand o2, _operand o3, opcode_opt opcodeOpt) => throw new NotImplementedException();
    internal void embed(_ptr data, int size) => throw new NotImplementedException();
    internal void bind(int labelId) => throw new NotImplementedException();
    internal void align(ALIGNING_MODE alignMode, int offset) => throw new NotImplementedException();
    internal void Emit(_opcode instructionId, _operand o0, INVALID_OPERAND iNVALID1, INVALID_OPERAND iNVALID2, INVALID_OPERAND iNVALID3, opcode_opt opcodeOpt) => throw new NotImplementedException();
}
