#pragma once
#include <cstdint>

struct stackval;
class WaveMethod;
struct WaveObject;

struct CallFrameException
{
    uint32_t* last_ip;
    WaveObject* value;
    wstring stack_trace;
};

struct CallFrame
{
    CallFrame* parent;
    WaveMethod* method;
    stackval* returnValue;
    void* _this_;
    stackval* args;
    stackval* stack;
    int level;

    CallFrameException* exception;


    static void fill_stacktrace(CallFrame* frame)
    {
        std::wstringstream s;

        s << fmt::format(L"\tat {0}{1}.{2}\n", frame->method->Owner->FullName->get_namespace(), frame->method->Owner->FullName->get_name(), frame->method->Name);

        auto r = frame->parent;

        while (r != nullptr)
        {
            s << fmt::format(L"\tat {0}{1}.{2}\n", r->method->Owner->FullName->get_namespace(), r->method->Owner->FullName->get_name(), r->method->Name);
            r = r->parent;
        }

        frame->exception->stack_trace = s.str();
    }
};