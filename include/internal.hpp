#pragma once
#include "compatibility.types.hpp"
#include "proxy.hpp"
#include "collections/create_map.hpp"
#include <map>

#include "string_storage.hpp"
using namespace std;

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
    return StringPool::Intern(L"Insomnia Windows");
#elif defined(__unix__)
    return WaveConvert<WaveString>::box(new WaveString("Insomnia Unix"));
#elif defined(__APPLE__)
    return WaveConvert<WaveString>::box(new WaveString("Insomnia Apple"));
#endif
};

#define FE_ARG(name) static const map<wstring, wstring> arg_list_of_##name = create_map<wstring, wstring>
#define GET_FE_ARG(name) arg_list_of_##name

/****************              start builtin              ****************/



#include "builtin/console.buniltin.hpp"



/****************              end builtin                ****************/

#include "../metadata/internal.args.def"

#define FE_CALL(name, type_name, link, argsize) internal_##name,

enum {
    #include "../metadata/internal.def"
    internal_last
};
#undef FE_CALL

#define FE_CALL(name, type_name, link, argsize) L#name,


static const wchar_t* internal_call_names[] = {
    #include "../metadata/internal.def"
    nullptr
};

#undef FE_CALL

#define FE_CALL(name, type_name, link, argsize) type_name,

static const wstring internal_call_functions_direction[] = {
    #include "../metadata/internal.def"
    L"<null>"
};

#undef FE_CALL

#define FE_CALL(name, type_name, link, argsize) &link,

static const wpointer internal_call_functions[] = {
    #include "../metadata/internal.def"
    nullptr
};
#undef FE_CALL

#define FE_CALL(name, type_name, link, argsize) argsize,

static const unsigned char internal_call_function_args_size[] = {
    #include "../metadata/internal.def"
    0
};

#undef FE_CALL

#define FE_CALL(name, type_name, link, argsize) &GET_FE_ARG(name),

//[[clang::no_destroy]]
static const void* internal_call_function_args_refs[] = {
    #include "../metadata/internal.def"
    nullptr
};

#undef FE_CALL