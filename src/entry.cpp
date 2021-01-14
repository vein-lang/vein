// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.h"
#include "interp.h"
#include "object.h"
#include "internal.h"
#include "types/WaveRuntimeType.h"

WaveImage* main_image = new WaveImage();

void setup() {
    init_serial();
    init_default();
    init_tables();
    /*auto a = new hashtable<const char*>();
    auto s1 = "xuy";
    auto s2 = "dick";

    auto z1 = &s1;
    auto z2 = &s2;

    a->add("1", z1);
    a->add("2", z2);

    auto z = *static_cast<const char**>(a->get("2"));*/
    /*auto a = new List<int*>(1);

    auto z1 = 1;
    auto z2 = 2;

    a->add(&z1);
    a->add(&z2);

    auto q1 = a->operator[](0);

    a->removeAt(0);

    auto q2 = a->operator[](0);*/

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
    main_image->method_cache = new hashtable<nativeString>();

    auto* str_1 = new WaveString("get_Platform");
    auto calle_f = hash_gen<WaveString>::getHashCode(str_1);

    main_image->db_str_cache->add(calle_f, str_1);

    unsigned char code[] = {
        LDC_I4_S, /* load i4 into stack                             */
        4,        /* i4                                             */
        LDC_I4_S, /* load i4 into stack                             */
        1,        /* i4                                             */
        ADD,      /* fetch two i4 and sum result push into stack    */
        LDC_I4_S, /* load i4 into stack                             */
        2,        /* i4                                             */
        XOR,      /* fetch i4 and XOR result push into stack        */
        DUMP_0,   /* debug                                          */
        LDARG_2,  /* load from args by 2 index into stack           */
        CALL,     /* call function by next index                    */
        static_cast<unsigned char>(calle_f),
        RET       /* return                                         */
    };

    auto* method = new WaveMethod();

    method->signature = new WaveMethodSignature();
    method->signature->call_convention = WAVE_CALL_DEFAULT;
    method->signature->param_count = 1;
    method->signature->ret = new WaveRuntimeType();
    method->signature->ret->type = TYPE_VOID;
    method->signature->ret->data.klass = wave_core->void_class;
    method->data.header = new MetaMethodHeader();
    method->data.header->max_stack = 24;
    method->data.header->code_size = sizeof(code);
    method->data.header->code = &*code;


    main_image->method_cache->add("main", method);

    auto* str = new WaveString("hello world, from wave vm!");

    auto* const args = new stackval[3];
    args[0].type = VAL_FLOAT;
    args[0].data.f_r4 = 14.48f;

    args[1].type = VAL_FLOAT;
    args[1].data.f_r4 = static_cast<float>(1483);

    args[2].type = VAL_OBJ;
    args[2].data.p = reinterpret_cast<size_t>(static_cast<void*>(str));


    auto* str2 = static_cast<WaveString*>(reinterpret_cast<void*>(args[2].data.p));

    exec_method(method->data.header, args);
}

void loop() {
}

#define SWITCH(x) d_print("@"); d_print(opcodes[x]); d_print("\n"); switch (x)

void exec_method(MetaMethodHeader* mh, stackval* args)
{
    w_print("@exec::");
    auto* const stack = static_cast<stackval*>(calloc(mh->max_stack, sizeof(stackval)));
    REGISTER auto* sp = stack;
    REGISTER auto* ip = mh->code;
    REGISTER unsigned int level = 0;
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
                auto* method_name = static_cast<WaveString*>(main_image->db_str_cache->get(static_cast<size_t>(*ip)));
                auto* method = static_cast<WaveMethod*>(wave_core->corlib->method_cache->get(method_name->chars));
                if(method->signature->call_convention == WAVE_CALL_C)
                {
                    auto* p_function = method->data.piinfo->addr;
                    switch (method->signature->param_count)
                    {
                        case 0:
                            p_function();
                            break;
                    }
                }
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

