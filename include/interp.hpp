#pragma once

#define ALLOC(t, x) ((t)__builtin_alloca(x))

#define OP_DEF(x, y, z) x = y,
enum {
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

	stackval* operator<<(WaveObject* obj)
	{
		this->type = VAL_OBJ;
		this->data.p = reinterpret_cast<size_t>(obj);
		return this;
	}
	
};



void exec_method(MetaMethodHeader* mh, stackval* args, unsigned int* level = 0);


#define A_OPERATION(op) ++ip; \
	--sp; \
    if (sp->type == VAL_I32) \
        sp[-1].data.i op sp[0].data.i; \
	else if (sp->type == VAL_I64) \
		sp[-1].data.l op sp[0].data.l; \
	else if (sp->type == VAL_DOUBLE) \
		sp[-1].data.f op sp[0].data.f; \
    else if (sp->type == VAL_FLOAT) \
		sp[-1].data.f_r4 op sp[0].data.f_r4

#define I_OPERATION(op) ++ip; \
	--sp; \
    if (sp->type == VAL_I32) \
        sp[-1].data.i op sp[0].data.i; \
	else if (sp->type == VAL_I64) \
		sp[-1].data.l op sp[0].data.l

#define DUMP_STACK(sp, idx) f_print(sp[idx].type); \
    if (sp[idx].type == VAL_I32) \
        f_print(sp[idx].data.i); \
    else if (sp[idx].type == VAL_DOUBLE) \
        f_print(sp[idx].data.f); \
    else if (sp[idx].type == VAL_I64) \
        f_print(sp[idx].data.l); \
    else if (sp[idx].type == VAL_FLOAT) \
        f_print(sp[idx].data.f_r4); \
    else if (sp[idx].type == VAL_OBJ) \
        f_print("<object>")