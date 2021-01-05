// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.h"
#include "interp.h"
#include "object.h"
#include "collections/hashtable.h"
#include "collections/list.h"

void setup() {
    d_init();
    /*auto a = new hashtable<const char*>();
    auto s1 = "xuy";
    auto s2 = "dick";

    auto z1 = &s1;
    auto z2 = &s2;

    a->add("1", z1);
    a->add("2", z2);

    auto z = *static_cast<const char**>(a->get("2"));*/
    auto a = new List<int*>(1);

    auto z1 = 1;
    auto z2 = 2;

    a->add(&z1);
    a->add(&z2);

    auto q1 = a->operator[](0);

    a->removeAt(0);

    auto q2 = a->operator[](0);

    /*auto a1 = hash_table_new(w_hash_str, w_equal_str);
    auto insertedKey = 0x1;
    auto val = "foo_string";
    hash_table_insert(a1, &insertedKey, static_cast<void*>(&val), false);

    auto res = hash_table_find(a1, [](const wpointer key, wpointer _) { return *static_cast<int*>(key) == 0x1; });

    const auto* res2 = static_cast<const char*>(res);*/
    /*
    auto fs = EEPROM();

    auto a1 = fs.write(0x0, 0x0);
    auto a2 = fs.write(0x1, 0x1);
    auto a3 = fs.write(0x2, 0x2);
    */
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
            case NOP:
                ASM("nop");
                ++ip;
                break;
            case ADD:
                A_OPERATION(+=);
                break;
            case SUB:
                A_OPERATION(-=);
                break;
            case MUL:
                A_OPERATION(*=);
                break;
            case DIV:
                A_OPERATION(/=);
                break;
            case XOR:
                I_OPERATION(^=);
                break;
            case AND:
                I_OPERATION(|=);
                break;
            case SHR:
                I_OPERATION(>>=);
                break;
            case SHL:
                I_OPERATION(<<=);
                break;
            case DUP:
                * sp = sp[-1];
                ++sp;
                ++ip;
                break;
            case LDARG_0:
            case LDARG_1:
            case LDARG_2:
            case LDARG_3:
            case LDARG_4:
                *sp = args[(*ip) - LDARG_0];
                ++sp;
                ++ip;
                break;
            case LDC_I4_0:
                ++ip;
                sp->type = VAL_I32;
                sp->data.i = -1;
                ++sp;
                break;
            case LDC_I4_S:
                ++ip;
                sp->type = VAL_I32;
                sp->data.i = static_cast<int32_t>(*ip);
                ++ip;
                ++sp;
                break;
            case DUMP_0:
                ++ip;
                DUMP_STACK(sp, -1);
                break;
            case DUMP_1:
                ++ip;
                DUMP_STACK(sp, 0);
                break;
            case RET:
                ++ip;
                return;
            case CALL:
                ++ip;
                break;
            case LDLOC_0:
            case LDLOC_1:
            case LDLOC_2:
            case LDLOC_3:
            case LDLOC_4:
                * sp = locals[(*ip) - LDLOC_0];
                ++ip;
                ++sp;
                break;
            case STLOC_0:
            case STLOC_1:
            case STLOC_2:
            case STLOC_3:
            case STLOC_4:
                --sp;
                locals[(*ip) - STLOC_0] = *sp;
                ++ip;
                break;
            case LOC_INIT:
                ++ip;
                locals = new stackval[args[*ip].data.i];
                ++sp;
                ++ip;
                break;
            case CONV_R4:
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

