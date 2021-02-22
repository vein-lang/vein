// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.hpp"
#include "interp.hpp"
#include "internal.hpp"
#include "api/elf_reader.hpp"
#include "emit/module_reader.hpp"
#include <fmt/format.h>
enum class CALL_CONTEXT : unsigned char
{
    INTERNAL_CALL,
    SELF_CALL,
    OUTER_CALL
};

void setup(int argc, char* argv[]) {
    init_serial();
    init_default();
    init_strings_phase_1();
    init_types();
    init_tables();
    init_strings_phase_2();
    auto* val = 
        //readILfromElf("C:\\Program Files (x86)\\WaveLang\\sdk\\0.1-preview\\runtimes\\any\\stl.wll");
        readILfromElf("C:\\Users\\ls-mi\\Desktop\\satl.wll");
    auto list = new list_t<WaveModule*>();
    list->push_back(wave_core->corlib);
    auto m = readModule(val->bytes.data(), val->size, list);
    /*if (argc == 1)
    {
        printf("[WARN] entry point not found.");
        return;
    }
    if (!std::string_view(argv[1]).ends_with(".wll"))
    {
        printf("[WARN] entry point not found.");
        return;
    }*/
    
    auto* entry_point = m->GetEntryPoint();
    auto* const args = new stackval[0];
   /* main_image->method_cache->add("main", method);

    auto* str = new WaveString("hello world, from wave vm!");

    auto* const args = new stackval[3];
    args[0].type = VAL_FLOAT;
    args[0].data.f_r4 = 14.48f;

    args[1].type = VAL_FLOAT;
    args[1].data.f_r4 = static_cast<float>(1483);

    args[2].type = VAL_OBJ;
    args[2].data.p = reinterpret_cast<size_t>(static_cast<void*>(str));


    auto* str2 = static_cast<WaveString*>(reinterpret_cast<void*>(args[2].data.p));
    */
    REGISTER unsigned int level = 0;

    exec_method(entry_point->data.header, args, &level);
}

void loop() {
}

#define SWITCH(x) d_print("@"); d_print(opcodes[x]); d_print("\n"); switch (x)

#define CASE(x) case x: {
#define BREAK break; }


WaveMethod* get_wave_method(uint32_t idx, WaveImage* targetImage)
{
    return nullptr;
}

void exec_method(MetaMethodHeader* mh, stackval* args, unsigned int* level)
{
    w_print("@exec::");
    auto* const stack = static_cast<stackval*>(calloc(mh->max_stack, sizeof(stackval)));
    REGISTER auto* sp = stack;
    REGISTER auto* ip = mh->code;
    
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

                #if DEBUG
                {
                    auto __type = VAL_NAMES[sp->type];
                    d_print("load from args -> ");
                    d_print(__type);
                    d_print("\n");
                }
                #endif

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
                (*level)--;
                return;
            case CALL:
            {
                ++ip;
                auto callctx = static_cast<CALL_CONTEXT>(static_cast<uint32_t>(*ip));

                if (callctx == CALL_CONTEXT::SELF_CALL)
                {
                    ++ip;
                   // auto* method = get_wave_method(*ip, main_image);
                    d_print("call ");
                    //d_print(method->name);
                    d_print(" self function.\n");
                    (*level)++;
                    //exec_method(method->data.header, args, level);

                    continue;
                }

                ++ip;
                //auto* method = get_wave_method(*ip, wave_core->corlib);

                /*d_print("call ");
                d_print(method->name);
                d_print(" internal function.\n");
                if (method->signature->call_convention == WAVE_CALL_C)
                {
                    WaveObject* f_result = nullptr;
                    auto* p_function = method->data.piinfo->addr;
                    switch (method->signature->param_count)
                    {
                        CASE(0)
                            f_result = static_cast<PInvokeDelegate0*>(p_function)();
                        BREAK;
                        CASE(1)
                            if (sp[-1].type != VAL_OBJ)
                                vm_shutdown();
                        auto* arg1 = static_cast<WaveObject*>(reinterpret_cast<void*>(sp[-1].data.p));
                        f_result =
                            static_cast<PInvokeDelegate1<WaveObject>*>(p_function)(arg1);
                        --sp;
                        BREAK;
                        CASE(2)
                            if (sp[-1].type != VAL_OBJ && sp[-2].type != VAL_OBJ)
                                vm_shutdown();

                        auto* arg1 = static_cast<WaveObject*>(reinterpret_cast<void*>(sp[-1].data.p));
                        auto* arg2 = static_cast<WaveObject*>(reinterpret_cast<void*>(sp[-2].data.p));
                        f_result =
                            static_cast<PInvokeDelegate2<WaveObject, WaveObject>*>(p_function)(arg1, arg2);
                        --sp;
                        --sp;
                        BREAK;
                    default:
                        vm_shutdown();
                        break;
                    }
                    if (f_result != nullptr)
                    {
                        sp->type = VAL_OBJ;
                        sp->data.p = reinterpret_cast<size_t>(f_result);
                    }
                }
                ++ip;*/
            }
            break;
            case LDLOC_0:
            case LDLOC_1:
            case LDLOC_2:
            case LDLOC_3:
            case LDLOC_4:
                * sp = locals[(*ip) - LDLOC_0];
                #if DEBUG
                {
                    auto __type = VAL_NAMES[sp->type];
                    d_print("load from locals -> ");
                    d_print(__type);
                    d_print("\n");
                }
                #endif
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
                #if DEBUG
                {
                    auto __type = VAL_NAMES[sp->type];
                    d_print("load into locals -> ");
                    d_print(__type);
                    d_print("\n");
                }
                #endif
                ++ip;
                break;
            case LOC_INIT:
                ++ip;
                locals = new stackval[args[*ip].data.i];
                d_print(args[*ip].data.i);
                w_print("'n locals size inited.");
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

