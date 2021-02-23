#pragma once


#define ALLOC(t, x) ((t)__builtin_alloca(x))

#define OP_DEF(x, y, z) x = y,
enum WaveOpCode {
    #include "../metadata/opcodes.def"
	LAST
};
#undef OP_DEF
#define OP_DEF(x, y, z) #x,
inline const char* opcodes [] = {
	#include "../metadata/opcodes.def"
	"LAST"
};
#undef OP_DEF
#define OP_DEF(x, y, z) z,
inline const unsigned char opcode_size [] = {
	#include "../metadata/opcodes.def"
	0
};
#undef OP_DEF

enum {
	VAL_I32,
	VAL_I64,
	VAL_DOUBLE,
	VAL_FLOAT,
	VAL_OBJ
};

static const char* VAL_NAMES[] = {
	"VAL_I32",
	"VAL_I64",
	"VAL_DOUBLE",
	"VAL_FLOAT",
	"VAL_OBJ"
};

struct stackval {
	union {
		int i;
		long l;
		float f_r4;
		double f;
		size_t p;
	} data;
	int type;
};