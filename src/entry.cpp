#include "core.h"
#include "stack.h"
#include "interp.h"
#include "object.h"



void setup() {
    d_init();

    const auto code_size = 5;
    const auto code = new unsigned char[code_size];

    code[0] = NOP;
    code[1] = LDARG_0;
    code[2] = LDARG_1;
    code[3] = ADD;
    code[4] = DUMP_0;

    const auto meta = new MetaMethodHeader();
    meta->max_stack = 24;
    meta->code_size = code_size;
    meta->code = code;

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
    register stackval* sp = stack;
    register unsigned char* ip = mh->code;
    register unsigned int level = 0;

    auto* end = ip + mh->code_size;

    stackval locals[16];
    while (1)
    {
        #if DEBUG && !ARDUINO
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
                * sp = args[(*ip) - LDARG_0];
            ++sp;
            ++ip;
            break;

            CASE(LDC_I32_0)
                ++ip;
            sp->type = VAL_I32;
            sp->data.i = -1;
            ++sp;
            break;
            CASE(DUMP_0)
                ++ip;
            f_print(sp[-1].type);
            f_print(sp[-1].data.i);
            CASE(HALT)
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