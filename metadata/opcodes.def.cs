/// <auto-generated> don't touch this file, for modification use gen.csx </auto-generated>
namespace ishtar;

[lang.c.CTypeExport("ishtar_opcode_e")]
[lang.c.CEnumPrefix("OPCODE_")]
public enum OpCodeValue : ushort 
	{
	/// <summary>
	/// Nope operation.
	/// </summary>
	NOP = 0x00,
	/// <summary>
	/// Add operation.
	/// </summary>
	ADD = 0x01,
	/// <summary>
	/// Substract operation.
	/// </summary>
	SUB = 0x02,
	/// <summary>
	/// Divide operation.
	/// </summary>
	DIV = 0x03,
	/// <summary>
	/// Multiple operation.
	/// </summary>
	MUL = 0x04,
	/// <summary>
	/// Modulo operation.
	/// </summary>
	MOD = 0x05,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_0 = 0x06,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_1 = 0x07,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_2 = 0x08,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_3 = 0x09,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_4 = 0x0A,
	/// <summary>
	/// Load into stack from argument.
	/// </summary>
	LDARG_5 = 0x0B,
	/// <summary>
	/// Load into stack from argument by index.
	/// </summary>
	LDARG_S = 0x0C,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_0 = 0x0D,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_1 = 0x0E,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_2 = 0x0F,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_3 = 0x10,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_4 = 0x11,
	/// <summary>
	/// Stage into argument from stack.
	/// </summary>
	STARG_5 = 0x12,
	/// <summary>
	/// Stage into argument from stack by index.
	/// </summary>
	STARG_S = 0x13,
	/// <summary>
	/// Load constant into stack.
	/// </summary>
	LDC_F4 = 0x14,
	/// <summary>
	/// Load constant into stack.
	/// </summary>
	LDC_F2 = 0x15,
	/// <summary>
	/// Load constant into stack.
	/// </summary>
	LDC_STR = 0x16,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_0 = 0x17,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_1 = 0x18,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_2 = 0x19,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_3 = 0x1A,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_4 = 0x1B,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_5 = 0x1C,
	/// <summary>
	/// Load int32 constant into stack.
	/// </summary>
	LDC_I4_S = 0x1D,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_0 = 0x1E,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_1 = 0x1F,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_2 = 0x20,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_3 = 0x21,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_4 = 0x22,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_5 = 0x23,
	/// <summary>
	/// Load int16 constant into stack.
	/// </summary>
	LDC_I2_S = 0x24,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_0 = 0x25,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_1 = 0x26,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_2 = 0x27,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_3 = 0x28,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_4 = 0x29,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_5 = 0x2A,
	/// <summary>
	/// Load int64 constant into stack.
	/// </summary>
	LDC_I8_S = 0x2B,
	/// <summary>
	/// Load float64 constant into stack.
	/// </summary>
	LDC_F8 = 0x2C,
	/// <summary>
	/// Load float128 constant into stack.
	/// </summary>
	LDC_F16 = 0x2D,
	/// <summary>
	/// Reserved operation.
	/// </summary>
	RESERVED_0 = 0x2E,
	/// <summary>
	/// Reserved operation.
	/// </summary>
	RESERVED_1 = 0x2F,
	/// <summary>
	/// Reserved operation.
	/// </summary>
	RESERVED_2 = 0x30,
	/// <summary>
	/// Return operation.
	/// </summary>
	RET = 0x31,
	/// <summary>
	/// Call operation.
	/// </summary>
	CALL = 0x32,
	/// <summary>
	/// Call operation (load pointer from stack).
	/// </summary>
	CALL_SP = 0x72,
	/// <summary>
	/// Load function pointer into stack.
	/// </summary>
	LDFN = 0x73,
	/// <summary>
	/// Load NULL into stack.
	/// </summary>
	LDNULL = 0x33,
	/// <summary>
	/// Load value from field in instance into stack.
	/// </summary>
	LDF = 0x34,
	/// <summary>
	/// Load value from static field into stack.
	/// </summary>
	LDSF = 0x35,
	/// <summary>
	/// Stage into instance field value from stack.
	/// </summary>
	STF = 0x36,
	/// <summary>
	/// Stage into static field value from stack.
	/// </summary>
	STSF = 0x37,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_0 = 0x38,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_1 = 0x39,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_2 = 0x3A,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_3 = 0x3B,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_4 = 0x3C,
	/// <summary>
	/// Load from locals into stack.
	/// </summary>
	LDLOC_5 = 0x3D,
	/// <summary>
	/// 
	/// </summary>
	LDLOC_S = 0x3E,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_0 = 0x3F,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_1 = 0x40,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_2 = 0x41,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_3 = 0x42,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_4 = 0x43,
	/// <summary>
	/// Load from stack into locals.
	/// </summary>
	STLOC_5 = 0x44,
	/// <summary>
	/// 
	/// </summary>
	STLOC_S = 0x45,
	/// <summary>
	/// Initialization locals stack.
	/// </summary>
	LOC_INIT = 0x46,
	/// <summary>
	/// (part of LOD.INIT) Initialization locals slot as derrived type.
	/// </summary>
	LOC_INIT_X = 0x47,
	/// <summary>
	/// Duplicate memory from stack.
	/// </summary>
	DUP = 0x48,
	/// <summary>
	/// Pop value from stack.
	/// </summary>
	POP = 0x69,
	/// <summary>
	/// Allocate memory block.
	/// </summary>
	ALLOC_BLOCK = 0x6A,
	/// <summary>
	/// Leave from protected zone.
	/// </summary>
	SEH_LEAVE_S = 0x6C,
	/// <summary>
	/// Leave from protected zone.
	/// </summary>
	SEH_LEAVE = 0x6D,
	/// <summary>
	/// End of finally statement.
	/// </summary>
	SEH_FINALLY = 0x6E,
	/// <summary>
	/// End of filter statement.
	/// </summary>
	SEH_FILTER = 0x6F,
	/// <summary>
	/// Enter protected zone.
	/// </summary>
	SEH_ENTER = 0x70,
	/// <summary>
	/// Free memory at point in stack.
	/// </summary>
	DELETE = 0x6B,
	/// <summary>
	/// XOR Operation.
	/// </summary>
	XOR = 0x49,
	/// <summary>
	/// OR Operation.
	/// </summary>
	OR = 0x4A,
	/// <summary>
	/// AND Operation.
	/// </summary>
	AND = 0x4B,
	/// <summary>
	/// Shift Right Operation.
	/// </summary>
	SHR = 0x4C,
	/// <summary>
	/// Shift Left Operation.
	/// </summary>
	SHL = 0x4D,
	/// <summary>
	/// Convertation operation.
	/// </summary>
	CONV_R4 = 0x4E,
	/// <summary>
	/// Convertation operation.
	/// </summary>
	CONV_R8 = 0x4F,
	/// <summary>
	/// Convertation operation.
	/// </summary>
	CONV_I4 = 0x50,
	/// <summary>
	/// Throw exception operation.
	/// </summary>
	THROW = 0x51,
	/// <summary>
	/// New object Operation.
	/// </summary>
	NEWOBJ = 0x52,
	/// <summary>
	/// Cast to T
	/// </summary>
	CAST = 0x71,
	/// <summary>
	/// Cast from T1 to T2
	/// </summary>
	CAST_G = 0x75,
	/// <summary>
	/// Allocate array onto evaluation stack by specified size and type.
	/// </summary>
	NEWARR = 0x53,
	/// <summary>
	/// Load size of Array onto evaluation stack.
	/// </summary>
	LDLEN = 0x54,
	/// <summary>
	/// 
	/// </summary>
	LDELEM_S = 0x55,
	/// <summary>
	/// 
	/// </summary>
	STELEM_S = 0x56,
	/// <summary>
	/// Load type token.
	/// </summary>
	LD_TYPE = 0x57,
	/// <summary>
	/// Load generic type argument token.
	/// </summary>
	LD_TYPE_G = 0x74,
	/// <summary>
	/// Take value from stack and load type.
	/// </summary>
	LD_TYPE_E = 0x76,
	/// <summary>
	/// Compare two value, when first value is less than or equal to second value stage 1 (int32) into stack, otherwise 0 (int32). (a <= b)
	/// </summary>
	EQL_LQ = 0x58,
	/// <summary>
	/// Compare two value, when first value is less second value stage 1 (int32) into stack, otherwise 0 (int32). (a < b)
	/// </summary>
	EQL_L = 0x59,
	/// <summary>
	/// Compare two value, when first value is greater than or equal to second value stage 1 (int32) into stack, otherwise 0 (int32). (a >= b)
	/// </summary>
	EQL_HQ = 0x5A,
	/// <summary>
	/// Compare two value, when first value is greater second value stage 1 (int32) into stack, otherwise 0 (int32). (a > b)
	/// </summary>
	EQL_H = 0x5B,
	/// <summary>
	/// Compare two value, when two integer/float is equal stage 1 (int32) into stack, otherwise 0 (int32). (a == b)
	/// </summary>
	EQL_NQ = 0x5C,
	/// <summary>
	/// Compare two value, when two integer/float is not equal stage 1 (int32) into stack, otherwise 0 (int32). (a != b)
	/// </summary>
	EQL_NN = 0x5D,
	/// <summary>
	/// Compare two value, when value has false, null or zero stage 1 (int32) into stack, otherwise 0 (int32). (!a)
	/// </summary>
	EQL_F = 0x5E,
	/// <summary>
	/// Compare two value, when value has true or either differs from null or from zero stage 1 (int32) into stack, otherwise 0 (int32). (a)
	/// </summary>
	EQL_T = 0x5F,
	/// <summary>
	/// Control flow, jump onto label. (unconditional)
	/// </summary>
	JMP = 0x60,
	/// <summary>
	/// Control flow, jump onto label when first value is less than or equal to second value. (a <= b)
	/// </summary>
	JMP_LQ = 0x61,
	/// <summary>
	/// Control flow, jump onto label when first value is less second value. (a < b)
	/// </summary>
	JMP_L = 0x62,
	/// <summary>
	/// Control flow, jump onto label when first value is greater than or equal to second value. (a >= b)
	/// </summary>
	JMP_HQ = 0x63,
	/// <summary>
	/// Control flow, jump onto label when first value is greater second value. (a > b)
	/// </summary>
	JMP_H = 0x64,
	/// <summary>
	/// Control flow, jump onto label when two integer/float is equal. (a == b)
	/// </summary>
	JMP_NQ = 0x65,
	/// <summary>
	/// Control flow, jump onto label when two integer/float is not equal. (a != b)
	/// </summary>
	JMP_NN = 0x66,
	/// <summary>
	/// Control flow, jump onto label when value has false, null or zero. (!a)
	/// </summary>
	JMP_F = 0x67,
	/// <summary>
	/// Control flow, jump onto label when value has true or either differs from null or from zero. (a)
	/// </summary>
	JMP_T = 0x68,

}
