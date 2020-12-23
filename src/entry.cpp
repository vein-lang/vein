// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.h"
#include "stack.h"
#include "interp.h"
#include "object.h"



void setup() {
    d_init();
    
    unsigned char code[] =  {
        NOP,
        LDARG_0,
        LDARG_1,
        ADD,
        DUMP_0,
        LDC_I4_S,
        228,
        DUMP_0,
        RET
    };
    const auto meta = new MetaMethodHeader();
    meta->max_stack = 24;
    meta->code_size = sizeof(code);
    meta->code = &*code;

    auto* const args = new stackval[2];
    args[0].type = VAL_I32;
    args[0].data.i = 12;
    args[1].type = VAL_I32;
    args[1].data.i = 14;
    exec_method(meta, args);
}

void loop() {
}

#define SWITCH(x) d_print("@"); d_print(opcodes[x]); d_print("\n"); switch (x)

void exec_method(MetaMethodHeader* mh, stackval* args)
{
    w_print("@exec::");
    auto* const stack = cast_t<stackval*>(calloc(mh->max_stack, sizeof(stackval)));
    register auto sp = stack;
    register auto ip = mh->code;
    register unsigned int level = 0;

    auto* end = ip + mh->code_size;

    stackval locals[16];
    while (1)
    {
        #if defined(DEBUG) && !defined(ARDUINO)
        {
            for (auto h = 0; h < level; ++h)
                d_print("\t");
        }
        printf("0x%04x %02x\n", ip - (unsigned char*)mh->code, *ip);
        if (sp != stack) {
            printf("[%d] %d 0x%08x %0.5f\n", sp - stack, sp[-1].type,
                sp[-1].data.i, sp[-1].data.f);
        }
        #endif
        SWITCH(*ip)
        {
            CASE(NOP)
                ASM("nop");
                ++ip;
                break;
            CASE(ADD)
                A_OPERATION(+= );
                break;
            CASE(SUB)
                A_OPERATION(-= );
                break;
            CASE(MUL)
                A_OPERATION(*= );
                break;
            CASE(DIV)
                A_OPERATION(/= );
                break;
            CASE(LDARG_0)
            CASE(LDARG_1)
            CASE(LDARG_2)
            CASE(LDARG_3)
            CASE(LDARG_4)
                *sp = args[(*ip) - LDARG_0];
                ++sp;
                ++ip;
                break;
            CASE(LDC_I4_0)
                ++ip;
                sp->type = VAL_I32;
                sp->data.i = -1;
                ++sp;
                break;
            CASE(LDC_I4_S)
                ++ip;
                sp->type = VAL_I32;
                sp->data.i = *ip; /* FIXME: signed? */
                ++ip;
                ++sp;
                break;
            CASE(DUMP_0)
                ++ip;
                DUMP_STACK(sp, -1);
                break;
            CASE(DUMP_1)
                ++ip;
                DUMP_STACK(sp, 0);
                break;
            CASE(RET)
                ++ip;
                return;
            CASE(CALL)
                ++ip;
                break;
            default:
                d_print("Unimplemented opcode: ");
                d_print(*ip);
                d_print("\n");
                return;
        }
    }


}

