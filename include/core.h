#pragma once
#include "compatibility.h"
#include "internal.h"
#include "collections/hashtable.h"


static hashtable<const char*>* functions_table = new hashtable<const char*>();
static hashtable<const char*>* internal_functions = new hashtable<const char*>();



void init_default()
{
    
}


void init_tables()
{
    for (auto i = 0; i < internal_last; i++)
    {
        auto* const f = new WaveMethod();

        f->name = internal_call_names[i];
        f->signature = new WaveMethodSignature();
        f->signature->call_convention = WAVE_CALL_C;
        f->signature->param_count = internal_call_function_args_size[i];
        //f->signature->params = new WaveParam*[i];
        f->flags = 0x0;
        f->data.piinfo = new WaveMethodPInvokeInfo();
        f->data.piinfo->addr = internal_call_functions[i];
        internal_functions->add(f->name, f);
    }
}