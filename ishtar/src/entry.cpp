#include "core.hpp"
#include "interp.hpp"
#include "internal.hpp"
#include "api/elf_reader.hpp"
#include "emit/module_reader.hpp"
#include <fmt/format.h>


#include "CallFrame.hpp"
#include "api/kernel_panic.hpp"
#include "api/Stopwatch.hpp"
#include "fmt/color.h"

// ReSharper disable once CppUnusedIncludeDirective
#include "debug_string.impl.hpp"
#include "string_storage.hpp"

enum class CALL_CONTEXT : unsigned char
{
    NATIVE_CALL,
    THIS_CALL,
    STATIC_CALL,
    BACKWARD_CALL
};

WaveObject* AllocateWaveString(const wstring& str)
{
    return StringPool::Intern(str);
}

WaveObject* GetWaveException(const wstring& msg)
{
    auto* obj = new WaveObject(wave_core->exception_class);
    auto* field = wave_core->exception_class->FindFieldByLowName(L"message");

    obj->vtable[field->vtable_offset] = AllocateWaveString(msg);

    return obj;
}

WaveString* GetMessageFromException(WaveObject* ex)
{
    auto* field = wave_core->exception_class->FindFieldByLowName(L"message");
    return static_cast<WaveString*>(static_cast<WaveObject*>(ex->vtable[field->vtable_offset]));
}

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

    auto frame = new CallFrame();

    frame->args = args;
    frame->method = entry_point;
    frame->level = 0;


    namespace sw = stopwatch;
    sw::Stopwatch my_watch;
    exec_method(frame);
    auto duration_ms = my_watch.elapsed();
    std::cout << "Elapsed: " << duration_ms / 1000.0f << " seconds." << std::endl;
}

void loop() {
}
void exec_method(CallFrame* invocation)
{
    #ifdef DEBUG_IL
    printf("@exec::\n");
    #endif
    auto* _module = invocation->method->Owner->owner_module;
    auto* mh = invocation->method->data.header;
    auto* args = invocation->args;

    function get_const_string = [_module](const int w) {
        return _module->GetConstByIndex(w);
    };


    auto* const stack = static_cast<stackval*>(calloc(mh->max_stack, sizeof(stackval)));
    REGISTER auto* sp = stack;
    REGISTER auto* ip = mh->code;

    invocation->stack = stack;

    #pragma optimize("", off)
    auto* start = (ip + 1) - 1;
    auto* end = mh->code + mh->code_size;
    #pragma optimize("", on)
    auto* locals = new stackval[0];
    while (1)
    {
        if (get_kernel_data()->exception)
        {
            print(fg(fmt::color::crimson) | fmt::emphasis::bold,
             L"native exception was thrown. \n\t[{0}] \n\t'{1}'\n", wave_exception_names[get_kernel_data()->exception->code],
                get_kernel_data()->exception->msg);
            vm_shutdown();
            return;
        }
        //printf("op: %d, ip: %llu, end: %llu\n", (uint32_t)*ip, (size_t)ip, (size_t)end);
        if(ip == end)
        {
            w_print("unexpected end of executable memory.");
            vm_shutdown();
            return;
        }
        SWITCH(*ip)
        {
            case NOP:
                ASM("nop");
                printf(".NOP\n");
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
                invocation->returnValue = &*sp;
                delete stack;
                delete[] locals;
                return;
            case CALL:
            {
                ++ip;
                auto callctx = static_cast<CALL_CONTEXT>(*ip);

                if (callctx == CALL_CONTEXT::THIS_CALL)
                {
                    auto child_frame = new CallFrame();
                    ++ip;
                    const auto tokenIdx = READ32(ip);
                    auto owner = readTypeName(&ip, &get_const_string);
                    auto* method = _module->GetMethod(tokenIdx, owner);
                    #ifdef DEBUG_IL
                    printf("%%call %ws self function.\n", method->Name.c_str());
                    #endif
                    
                    auto* method_args = new stackval[method->ArgLen()];
                    for (auto i = 0; i != method->ArgLen(); i++)
                    {
                        auto* _a = method->Arguments->at(i);
                        // TODO, type eq validate
                        --sp;
                        method_args[i] = *sp;
                    }
                    child_frame->level = invocation->level + 1;
                    child_frame->parent = invocation;
                    child_frame->args = method_args;
                    child_frame->method = method;

                    exec_method(child_frame);
                    if (child_frame->exception)
                    {
                        auto* msg = GetMessageFromException(child_frame->exception->value);
                        //auto d2 = msg->GetValue();
                        print(fg(fmt::color::crimson) | fmt::emphasis::bold,
                         L"unhandled exception was thrown. \n [{0}] {1}\n{2}",
                            child_frame->exception->value->clazz->FullName->get_name(),
                            L"null reference exception",
                            child_frame->exception->stack_trace);
                        vm_shutdown();
                        return;
                    }
                    if (method->ReturnType->TypeCode != TYPE_VOID)
                    {
                        *sp = *child_frame->returnValue;
                        sp++;
                    }
                    

                    try
                    {
                        throw 228;
                    }
                    catch(int& ex)
                    {
                        
                    }

                    delete[] method_args;
                    delete child_frame;
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
                        auto* type_name = readTypeName(&ip, &get_const_string);
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

                    #undef OPERATOR
                    #define OPERATOR !=
                    S_JUMP(VAL_I8, b, OPERATOR)
                    S_JUMP(VAL_I16, s, OPERATOR)
                    S_JUMP(VAL_I32, i, OPERATOR)
                    S_JUMP(VAL_I64, l, OPERATOR)

                    S_JUMP(VAL_U8, ub, OPERATOR)
                    S_JUMP(VAL_U16, us, OPERATOR)
                    S_JUMP(VAL_U32, ui, OPERATOR)
                    S_JUMP(VAL_U64, ul, OPERATOR)

                    S_JUMP(VAL_FLOAT, f_r4, OPERATOR)
                    S_JUMP(VAL_DOUBLE, f, OPERATOR)

                    if (first.type == VAL_DECIMAL)
                        W_JUMP_AFTER(*first.data.d, OPERATOR, 0.0f);
                    else if (first.type == VAL_HALF)
                        W_JUMP_AFTER(*first.data.hf, OPERATOR, static_cast<half>(0.0f));
                    else if (first.type == VAL_OBJ)
                        W_JUMP_AFTER(first.data.p, OPERATOR, 0);
                    #undef OPERATOR
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
                        #undef OPERATOR
                        #define OPERATOR !=
                        W_JUMP(VAL_I8, b, OPERATOR)
                        W_JUMP(VAL_I16, s, OPERATOR)
                        W_JUMP(VAL_I32, i, OPERATOR)
                        W_JUMP(VAL_I64, l, OPERATOR)

                        W_JUMP(VAL_U8, ub, OPERATOR)
                        W_JUMP(VAL_U16, us, OPERATOR)
                        W_JUMP(VAL_U32, ui, OPERATOR)
                        W_JUMP(VAL_U64, ul, OPERATOR)

                        W_JUMP(VAL_FLOAT, f_r4, OPERATOR)
                        W_JUMP(VAL_DOUBLE, f, OPERATOR)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, OPERATOR, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, OPERATOR, *second.data.hf);

                        #undef OPERATOR
                    }
                    else
                        throw "not implemented exception";
                }
            break;
            case JMP:
                ++ip;
                W_JUMP_NOW();
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
                        #undef OPERATOR
                        #define OPERATOR <=
                        W_JUMP(VAL_I8, b, OPERATOR)
                        W_JUMP(VAL_I16, s, OPERATOR)
                        W_JUMP(VAL_I32, i, OPERATOR)
                        W_JUMP(VAL_I64, l, OPERATOR)

                        W_JUMP(VAL_U8, ub, OPERATOR)
                        W_JUMP(VAL_U16, us, OPERATOR)
                        W_JUMP(VAL_U32, ui, OPERATOR)
                        W_JUMP(VAL_U64, ul, OPERATOR)

                        W_JUMP(VAL_FLOAT, f_r4, OPERATOR)
                        W_JUMP(VAL_DOUBLE, f, OPERATOR)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, <=, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, <=, *second.data.hf);
                        #undef OPERATOR
                    }
                    else
                        throw "not implemented exception";
                }
            break;
            case JMP_HQ:
                {
                    ++ip;
                    --sp;
                    auto first = *sp;
                    --sp;
                    auto second = *sp;
                    if (first.type == second.type)
                    {
                        #undef OPERATOR
                        #define OPERATOR >=
                        W_JUMP(VAL_I8, b, OPERATOR)
                        W_JUMP(VAL_I16, s, OPERATOR)
                        W_JUMP(VAL_I32, i, OPERATOR)
                        W_JUMP(VAL_I64, l, OPERATOR)

                        W_JUMP(VAL_U8, ub, OPERATOR)
                        W_JUMP(VAL_U16, us, OPERATOR)
                        W_JUMP(VAL_U32, ui, OPERATOR)
                        W_JUMP(VAL_U64, ul, OPERATOR)

                        W_JUMP(VAL_FLOAT, f_r4, OPERATOR)
                        W_JUMP(VAL_DOUBLE, f, OPERATOR)

                        if (first.type == VAL_DECIMAL)
                            W_JUMP_AFTER(*first.data.d, OPERATOR, *second.data.d);
                        else if (first.type == VAL_HALF)
                            W_JUMP_AFTER(*first.data.hf, OPERATOR, *second.data.hf);
                        #undef OPERATOR
                    }
                    else
                        throw "not implemented exception";
                }
            break;
            case THROW:
                --sp;
                if (!sp->data.p)
				    sp->data.p = reinterpret_cast<size_t>(GetWaveException(L"null reference exception"));
                if (sp->type != TYPE_OBJECT)
                {
                    set_failure(WAVE_EXCEPTION_TYPE_LOAD, L"stack type is not TYPE_OBJECT.");
                    return;
                }
                invocation->exception = new CallFrameException();
                invocation->exception->value = reinterpret_cast<WaveObject*>(sp->data.p);
                invocation->exception->last_ip = ip;
                CallFrame::fill_stacktrace(invocation);
                return;
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