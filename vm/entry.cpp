// ReSharper disable CppDeprecatedRegisterStorageClassSpecifier
#include "core.hpp"
#include "interp.hpp"
#include "internal.hpp"
#include "api/elf_reader.hpp"
#include "emit/module_reader.hpp"
#include <fmt/format.h>
#include "api/Stopwatch.hpp"
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
        //readILfromElf("C:\\Users\\ls-mi\\Desktop\\satl.wll");
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
    auto* const args = new stackval[1];
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
    namespace sw = stopwatch;
    sw::Stopwatch my_watch;
    exec_method(entry_point->data.header, args, m, &level);
    auto duration_ms = my_watch.elapsed();
    std::cout << "Elapsed: " << duration_ms / 1000.0f << " seconds." << std::endl;
}

void loop() {
}





WaveMethod* get_wave_method(uint32_t idx, WaveImage* targetImage)
{
    return nullptr;
}

void exec_method(MetaMethodHeader* mh, stackval* args, WaveModule* _module, unsigned int* level)
{
    #ifdef DEBUG_IL
    printf("@exec::\n");
    #endif
    function<wstring(int z)> get_const_string = [_module](const int w) {
        return _module->GetConstByIndex(w);
    };


    auto* const stack = static_cast<stackval*>(calloc(mh->max_stack, sizeof(stackval)));
    REGISTER auto* sp = stack;
    REGISTER auto* ip = mh->code;
    
    auto* end = ip + mh->code_size;
    auto* start = (ip + 1) - 1;

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

                #ifdef DEBUG_IL
                printf("load from args -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
                #endif

                ++sp;
                ++ip;
                break;
            case LDC_I4_0:
            case LDC_I4_1:
            case LDC_I4_2:
            case LDC_I4_3:
            case LDC_I4_5:
                sp->type = VAL_I32;
                sp->data.i = (*ip) - LDC_I4_0;
                ++ip;
                ++sp;
                break;
            case LDC_I8_0:
            case LDC_I8_1:
            case LDC_I8_2:
            case LDC_I8_3:
            case LDC_I8_5:
                sp->type = VAL_I64;
                sp->data.l = (*ip) - LDC_I8_0;
                ++sp;
                ++ip;
                break;
            case LDC_I4_S:
                ++ip;
                sp->type = VAL_I32;
                sp->data.i = static_cast<int32_t>(*ip);
                ++ip;
                ++sp;
                break;
            case LDC_I8_S:
                ++ip;
                sp->type = VAL_I64;
                sp->data.l = static_cast<int32_t>(*ip);
                ++ip;
                ++sp;
                break;
            case DUMP_0:
                ++ip;
                DUMP_STACK(-1);
                break;
            case DUMP_1:
                ++ip;
                DUMP_STACK(0);
                
                break;
            case RET:
                ++ip;
                --sp;
                args[0] = *sp;
                (*level)--;
                delete stack;
                delete[] locals;
                return;
            case CALL:
            {
                ++ip;
                auto callctx = static_cast<CALL_CONTEXT>(static_cast<uint32_t>(*ip));

                if (callctx == CALL_CONTEXT::SELF_CALL)
                {
                    ++ip;
                    const auto tokenIdx = READ32(ip);
                    ip++;
                    const auto ownerIdx = READ64(ip);
                    ip+=2;

                    auto* method = _module->GetMethod(tokenIdx, ownerIdx);
                    #ifdef DEBUG_IL
                    printf("%%call %ws self function.\n", method->Name.c_str());
                    #endif
                    (*level)++;
                    auto* method_args = new stackval[method->ArgLen()];
                    for (auto i = 0; i != method->ArgLen(); i++)
                    {
                        auto* _a = method->Arguments->at(i);
                        // TODO, type eq validate
                        --sp;
                        method_args[i] = *sp;
                    }
                    exec_method(method->data.header, method_args, _module, level);
                    if (method->ReturnType->TypeCode != TYPE_VOID)
                    {
                        *sp = method_args[0];
                        sp++;
                    }
                    delete[] method_args;
                    break;
                }
                throw "not implemented";
                /*++ip;
                auto* method = get_wave_method(*ip, wave_core->corlib);
                d_print("call ");
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
                #ifdef DEBUG_IL
                printf("load from locals -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
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
                #ifdef DEBUG_IL
                printf("load from locals -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
                #endif
                ++ip;
                break;
            case LOC_INIT:
                {
                    ++ip;
                    const auto locals_size = *ip;
                    locals = new stackval[static_cast<int32_t>(locals_size)];
                    ++ip;
                    for (auto i = 0u; i != locals_size; i++)
                    {
                        ++ip; // skip opcode LOC_INIT_X
                        const auto type_idx = READ64(ip);
                        auto* type_name = TypeName::construct(type_idx, &get_const_string);
                        auto* type = _module->FindType(type_name, true);
                        if (type->IsPrimitive())
                        {
                            LOCALS_INIT(TYPE_I1, VAL_I8, i, b)
                            LOCALS_INIT(TYPE_I2, VAL_I16, i, s)
                            LOCALS_INIT(TYPE_I4, VAL_I32, i, i)
                            LOCALS_INIT(TYPE_I8, VAL_I64, i, l)

                            LOCALS_INIT(TYPE_U1, VAL_U8, i, ub)
                            LOCALS_INIT(TYPE_U2, VAL_U16, i, us)
                            LOCALS_INIT(TYPE_U4, VAL_U32, i, ui)
                            LOCALS_INIT(TYPE_U8, VAL_U64, i, ul)

                            LOCALS_INIT(TYPE_R4, VAL_FLOAT, i, f_r4)
                            LOCALS_INIT(TYPE_R8, VAL_DOUBLE, i, f)
                        }
                        else
                        {
                            locals[i].type = VAL_OBJ;
                            locals[i].data.p = 0;
                        }
                        ip += 2; // after READ64
                    }
                }
                break;
            case CONV_R4:
                ++ip;
                sp[-1].data.i = static_cast<int>(sp[-1].data.f_r4);
                sp[-1].type = VAL_I32;
                break;
            case JMP_L:
                {
                    ++ip;
                    --sp;
                    const auto first = *sp;
                    --sp;
                    const auto second = *sp;
                    if (first.type == second.type)
                    {
                        W_JUMP(VAL_I8, b, <)
                        W_JUMP(VAL_I16, s, <)
                        W_JUMP(VAL_I32, i, <)
                        W_JUMP(VAL_I64, l, <)

                        W_JUMP(VAL_U8, ub, <)
                        W_JUMP(VAL_U16, us, <)
                        W_JUMP(VAL_U32, ui, <)
                        W_JUMP(VAL_U64, ul, <)

                        W_JUMP(VAL_FLOAT, f_r4, <)
                        W_JUMP(VAL_DOUBLE, f, <)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, <, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, <, *second.data.hf);
                    }
                    else
                        throw "not implemented exception";
                }
                break;
            case JMP_T:
                {
                    ++ip;
                    --sp;
                    const auto first = *sp;
                    if (first.type == VAL_I32)
                    {
                        if (first.data.i != 0)
                            ip = start + (mh->labels_map->at(mh->labels->at(*ip))).pos;
                        else
                            ip++;
                    }
                    if (first.type == VAL_I64)
                    {
                        if (first.data.i != 0)
                            ip = start + (mh->labels_map->at(mh->labels->at(*ip))).pos;
                        else
                            ip++;
                    }
                }
            break;
            case JMP_NN:
                {
                    ++ip;
                    --sp;
                    const auto first = *sp;
                    --sp;
                    const auto second = *sp;
                    if (first.type == second.type)
                    {
                        W_JUMP(VAL_I8, b, !=)
                        W_JUMP(VAL_I16, s, !=)
                        W_JUMP(VAL_I32, i, !=)
                        W_JUMP(VAL_I64, l, !=)

                        W_JUMP(VAL_U8, ub, !=)
                        W_JUMP(VAL_U16, us, !=)
                        W_JUMP(VAL_U32, ui, !=)
                        W_JUMP(VAL_U64, ul, !=)

                        W_JUMP(VAL_FLOAT, f_r4, !=)
                        W_JUMP(VAL_DOUBLE, f, !=)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, !=, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, !=, *second.data.hf);
                    }
                    else
                        throw "not implemented exception";
                }
            break;
            case JMP:
                {
                    ++ip;
                    ip = start + (mh->labels_map->at(mh->labels->at(*ip))).pos;
                }
                break;
            case JMP_LQ:
                {
                    ++ip;
                    --sp;
                    auto first = *sp;
                    --sp;
                    auto second = *sp;
                    if (first.type == second.type)
                    {
                        W_JUMP(VAL_I8, b, <=)
                        W_JUMP(VAL_I16, s, <=)
                        W_JUMP(VAL_I32, i, <=)
                        W_JUMP(VAL_I64, l, <=)

                        W_JUMP(VAL_U8, ub, <=)
                        W_JUMP(VAL_U16, us, <=)
                        W_JUMP(VAL_U32, ui, <=)
                        W_JUMP(VAL_U64, ul, <=)

                        W_JUMP(VAL_FLOAT, f_r4, <=)
                        W_JUMP(VAL_DOUBLE, f, <=)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, <=, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, <=, *second.data.hf);
                    }
                    else
                        throw "not implemented exception";
                }
            break;
            default:
                {
                    d_print("Unimplemented opcode: ");
                    d_print(opcodes[*ip]);
                    d_print("\n");
                }
                return;
        }
    }


}