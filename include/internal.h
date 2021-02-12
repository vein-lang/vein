#pragma once
#include "compatibility.types.h"
#include "proxy.h"


using PInvokeDelegate0 = WaveObject*();
template<typename T1>
using PInvokeDelegate1 = WaveObject*(T1* t1);
template<typename T1, typename T2>
using PInvokeDelegate2 = WaveObject*(T1* t1, T2* t2);


static WaveObject* i_call_get_Platform()
{
#if defined(AVR_PLATFORM)
    return WaveConvert<WaveString>::box(new WaveString("Insomnia AVR"));
#elif defined(_WIN32) || defined(WIN32) 
    return WaveConvert<WaveString>::box(new WaveString("Insomnia Windows"));
#elif defined(__unix__)
    return WaveConvert<WaveString>::box(new WaveString("Insomnia Unix"));
#elif defined(__APPLE__)
    return WaveConvert<WaveString>::box(new WaveString("Insomnia Apple"));
#endif
};



#include "builtin/console.buniltin.h"

static WaveObject* i_call_printf(WaveString* str)
{
    d_print("i_call_printf:: ");
    w_print(str->chars);
    return nullptr;
}


static WaveObject* i_call_Echo()
{
    w_print("echo");
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