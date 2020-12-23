#pragma once
#include "types.h"

#define ALLOC(t, x) ((t)__builtin_alloca(x))

#define OP_DEF(x, y) x = y,
enum {
    #include "../metadata/opcodes.def"
	LAST = 0xff
};
#undef OP_DEF
#define OP_DEF(x, y) #x,
const char* opcodes [] = {
	#include "../metadata/opcodes.def"
	"LAST"
};
#undef OP_DEF

enum {
	VAL_I32,
	VAL_I64,
	VAL_DOUBLE,
	VAL_FLOAT,
	VAL_OBJ
};

typedef struct {
	union {
		int i;
		long l;
		struct {
			int lo;
			int hi;
		} pair;
		float f_r4;
		double f;
		size_t p;
	} data;
	int type;
} stackval;


void exec_method(MetaMethodHeader* mh, stackval* args);


#define CASE(x) case x:


template<class T>
T cast_t(void* v)
{
	return static_cast<T>(v);
}


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