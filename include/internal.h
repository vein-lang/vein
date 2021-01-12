#pragma once
#include "compatibility.types.h"
#include "types/WaveCore.h"
#include "types/WaveObject.h"


static WaveString* i_call_get_Platform()
{
    return new WaveString("wave_vm");
};

static WaveObject* i_call_printf(WaveString* str)
{
    w_print(str->chars);
    return nullptr;
}

#define INTERNAL_CALL(id, func, argsize) internal_ ## id,

enum {
    #include "../metadata/internal.def"
    internal_last
};
#undef INTERNAL_CALL

#define INTERNAL_CALL(id, func, argsize) #id,


static const char* internal_call_names[] = {
    #include "../metadata/internal.def"
    nullptr
};

#undef INTERNAL_CALL

#define INTERNAL_CALL(id, func, argsize) &func,

static const wpointer internal_call_functions[] = {
    #include "../metadata/internal.def"
    nullptr
};
#undef INTERNAL_CALL

#define INTERNAL_CALL(id, func, argsize) argsize,

static const byte internal_call_function_args_size[] = {
    #include "../metadata/internal.def"
    0
};