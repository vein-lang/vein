// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.h"
#include "interp.h"
#include "object.h"

void setup() {
    d_init();
    unsigned char code[] = {
        LDC_I4_S,
        4,
        LDC_I4_S,
        1,
        ADD,
        LDC_I4_S,
        2,
        XOR,
        DUMP_0,
        RET
    };
    const auto meta = new MetaMethodHeader();
    meta->max_stack = 24;
    meta->code_size = sizeof(code);
    meta->code = &*code;

    auto* const args = new stackval[2];
    args[0].type = VAL_FLOAT;
    args[0].data.f_r4 = 14.48f;

    args[1].type = VAL_FLOAT;
    args[1].data.f_r4 = static_cast<float>(1483);

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

    auto* locals = new stackval[0];
    while (1)
    {
        if(*ip == *end)
        {
            w_print("unexpected end of executable memory.");
            vm_shutdown();
        }
        #if !defined(DEBUG) && !defined(ARDUINO)
        {
            for (auto h = 0; h < level; ++h)
                d_print("\t");
        }
        printf("0x%04x %02x\n", ip - static_cast<unsigned char*>(mh->code), *ip);
        if (sp != stack) {
            printf("[%d] %d 0x%08x %0.5f %0.5f\n", sp - stack, sp[-1].type,
                sp[-1].data.i, sp[-1].data.f, sp[-1].data.f_r4);
        }
        #endif
        SWITCH(*ip)
        {
            CASE(NOP)
                ASM("nop");
                ++ip;
                break;
            CASE(ADD)
                A_OPERATION(+=);
                break;
            CASE(SUB)
                A_OPERATION(-=);
                break;
            CASE(MUL)
                A_OPERATION(*=);
                break;
            CASE(DIV)
                A_OPERATION(/=);
                break;
            CASE(XOR)
                I_OPERATION(^=);
                break;
            CASE(AND)
                I_OPERATION(|=);
                break;
            CASE(SHR)
                I_OPERATION(>>=);
                break;
            CASE(SHL)
                I_OPERATION(<<=);
                break;
            CASE(DUP)
                * sp = sp[-1];
                ++sp;
                ++ip;
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
                sp->data.i = static_cast<int32_t>(*ip);
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
            CASE(LDLOC_0)
            CASE(LDLOC_1)
            CASE(LDLOC_2)
            CASE(LDLOC_3)
            CASE(LDLOC_4)
                * sp = locals[(*ip) - LDLOC_0];
                ++ip;
                ++sp;
                break
            CASE(STLOC_0)
            CASE(STLOC_1)
            CASE(STLOC_2)
            CASE(STLOC_3)
            CASE(STLOC_4)
                --sp;
                locals[(*ip) - STLOC_0] = *sp;
                ++ip;
                break;
            CASE(LOC_INIT)
                ++ip;
                locals = new stackval[args[*ip].data.i];
                ++sp;
                ++ip;
                break;
            CASE(CONV_R4)
                ++ip;
                sp[-1].data.i = static_cast<int>(sp[-1].data.f_r4);
                sp[-1].type = VAL_I32;
                break;
            default:
                d_print("Unimplemented opcode: ");
                d_print(opcodes[*ip]);
                d_print("\n");
                return;
        }
    }


}

