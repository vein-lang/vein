#pragma once
#include "compatibility.types.h"
#include "collections/hashtable.h"

struct function_pointer {
    const void* pointer;
    const char* name;
    uint32_t hash;
};

#define INTERNAL_CALL(id, func) internal_ ## id,

enum {
    #include "../metadata/internal.def"
    internal_last
};
#undef INTERNAL_CALL

#define INTERNAL_CALL(id, func) #id,


static const char* internal_call_names[] = {
    #include "../metadata/internal.def"
    nullptr
};

#undef INTERNAL_CALL

#define INTERNAL_CALL(id, func) &func,

static const void* internal_call_functions[] = {
    #include "../metadata/internal.def"
    nullptr
};



static hashtable<const char*>* internal_functions;


class ICALL {
public:
    static void init()
    {
        internal_functions = new hashtable<const char*>();

        for (auto i = 0; i < internal_last; i++)
        {
            auto* const f = new function_pointer();
            f->name = internal_call_names[i];
            f->pointer = internal_call_functions[i];
            f->hash = hash_gen<const char*>::getHashCode(f->name);

            internal_functions->add(f->name, f);
        }
    }
    static function_pointer* get_method(const char* name)
    {
        return static_cast<function_pointer*>(internal_functions->get(name));
    }
};