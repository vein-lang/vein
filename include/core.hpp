#pragma once
#include "compatibility.hpp"
#include "internal.h"
#include "types/WaveCallConvention.h"
#include "types/WaveCore.hpp"
#include "types/WaveImage.hpp"
#include "types/WaveMethod.hpp"
#include "WaveTypeCode.hpp"

#define root_namespace "wave/lang"
#define class_name(namespace, class_name) "global::"#namespace#class_name

namespace __internal
{
    WaveClass* wave_create_object_class(WaveImage* core_image)
    {
        auto* t = new WaveClass();

        t->inited = true;
        t->type_token = TYPE_OBJECT;
        t->name = "Object";
        t->path = "wave/lang";
        t->parent = nullptr;

        core_image->class_cache->add(class_name(root_namespace, "Object"), t);
        return t;
    }

    WaveClass* wave_create_void_class(WaveImage* core_image, WaveClass* parent)
    {
        auto* t = new WaveClass();

        t->inited = true;
        t->type_token = TYPE_VOID;
        t->name = "Void";
        t->path = "wave/lang";
        t->parent = parent;

        core_image->class_cache->add(class_name(root_namespace, "Void"), t);

        return t;
    }
}


void init_default()
{
    wave_core->corlib = new WaveImage("wavecorlib");
    
    wave_core->object_class = __internal::wave_create_object_class(wave_core->corlib);
    wave_core->void_class = __internal::wave_create_void_class(wave_core->corlib, wave_core->object_class);
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
        wave_core->corlib->method_cache->add(f->name, f);
    }
}