#pragma once
#include "CallFrame.hpp"
#include "types/WaveMethodHeader.hpp"

void exec_method(CallFrame* invocation);



//#define DEBUG_IL

#ifdef DEBUG_IL
#define SWITCH(x) d_print("@"); d_print(opcodes[x]); d_print("\n"); switch (x)
#else
#define SWITCH(x) switch (x)
#endif

#define CASE(x) case x: {
#define BREAK break; }

#define A_OPERATION(op) ++ip; \
	--sp; \
    if (sp->type == VAL_I32) /*first check int32, most frequent type*/ \
        sp[-1].data.i op sp[0].data.i; \
    else if (sp->type == VAL_I64)  \
		sp[-1].data.l op sp[0].data.l;\
    else if (sp->type == VAL_I16) \
        sp[-1].data.s op sp[0].data.s;\
    else if (sp->type == VAL_I8) \
        sp[-1].data.b op sp[0].data.b; \
    else if (sp->type == VAL_U32) \
        sp[-1].data.ui op sp[0].data.ui; \
    else if (sp->type == VAL_U8) \
        sp[-1].data.ub op sp[0].data.ub; \
    else if (sp->type == VAL_U16) \
        sp[-1].data.us op sp[0].data.us; \
	else if (sp->type == VAL_U64) \
		sp[-1].data.ul op sp[0].data.ul;\
	else if (sp->type == VAL_DOUBLE) \
		sp[-1].data.f op sp[0].data.f; \
    else if (sp->type == VAL_FLOAT) \
		sp[-1].data.f_r4 op sp[0].data.f_r4;\
    else if (sp->type == VAL_DECIMAL) \
		*sp[-1].data.d op *sp[0].data.d;\
    else if (sp->type == VAL_HALF) \
		*sp[-1].data.hf op *sp[0].data.hf;\
    else if (sp[-1].type == VAL_INCORRECT)\
        printf("@%s 'sp[-1]' incorrect stack type: %d\n", opcodes[*(ip-1)], sp[-1].type)

#define I_OPERATION(op) ++ip; \
	--sp; \
    if (sp[-1].type == VAL_I32) \
        sp[-1].data.i op sp[0].data.i; \
	else if (sp[-1].type == VAL_I64) \
		sp[-1].data.l op sp[0].data.l; \
    else if (sp[-1].type == VAL_INCORRECT) \
        printf("@%s 'sp[-1]' incorrect stack type: %d\n", opcodes[*(ip-1)], sp[-1].type)

#define DUMP_STACK(idx) f_print(sp[idx].type); \
    if (sp[idx].type == VAL_I32) \
        f_print(sp[idx].data.i); \
    else if (sp[idx].type == VAL_DOUBLE) \
        f_print(sp[idx].data.f); \
    else if (sp[idx].type == VAL_I64) \
        f_print(sp[idx].data.l); \
    else if (sp[idx].type == VAL_I8) \
        f_print(sp[idx].data.b); \
    else if (sp[idx].type == VAL_U8) \
        f_print(sp[idx].data.ub); \
    else if (sp[idx].type == VAL_U16) \
        f_print(sp[idx].data.us); \
    else if (sp[idx].type == VAL_U32) \
        f_print(sp[idx].data.ui); \
    else if (sp[idx].type == VAL_U64) \
        f_print(sp[idx].data.ul); \
    else if (sp[idx].type == VAL_FLOAT) \
        f_print(sp[idx].data.f_r4); \
    else if (sp[idx].type == VAL_DECIMAL) \
        f_print(*sp[idx].data.d); \
    else if (sp[idx].type == VAL_HALF) \
        f_print(static_cast<float>(*sp[0].data.hf)); \
    else if (sp[idx].type == VAL_OBJ) \
        f_print("<object>"); \
    else if (sp[idx].type == VAL_INCORRECT) \
        printf("@%s 'sp[-1]' incorrect stack type: %d\n", opcodes[*(ip-1)], sp[idx].type)


#define LOCALS_INIT(t, v, index, i) if (type->TypeCode == t) \
{ \
    locals[index].type = v; \
    locals[index].data.i = 0; \
}


#ifdef DEBUG_IL
#define W_JUMP_DEBUG(F, S, O) printf(fmt::format("@{0} ({1}) [{2} {3} {4}] -> {5}\n", opcodes[*(ip-1)], #F, F, #S, O, F S O  ? "true" : "false").c_str())
#else
#define W_JUMP_DEBUG(F, S, O) do {} while(0)
#endif
#define W_JUMP_NOW() ip = start + (mh->labels_map->at(mh->labels->at(*ip))).pos
#define W_JUMP_AFTER(F,S,O) do { W_JUMP_DEBUG(F, S, O); if (F S O) W_JUMP_NOW(); else ip++; } while(0)
#define W_JUMP(v_type, variable, op) if (first.type == v_type) { \
    W_JUMP_AFTER(first.data.variable, op, second.data.variable); } \
    else
#define S_JUMP(v_type, variable, op) if (first.type == v_type) { \
    W_JUMP_AFTER(first.data.variable, op, 0); } \
    else