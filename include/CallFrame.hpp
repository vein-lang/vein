#pragma once
#include <cstdint>

struct stackval;
class WaveMethod;
struct WaveObject;

struct CallFrameException
{
    uint32_t* last_ip;
    WaveObject* value;
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
};