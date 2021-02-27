#pragma once
#include "types/WaveCore.hpp"
#include "types/WaveObject.hpp"
#include "types/WaveString.hpp"



template<class TIn> class WaveConvert {};


template<> class WaveConvert<WaveString> {
public:
    static WaveObject* box(WaveString* s) {
        auto* obj = new WaveObject();
        obj->data = s;
        obj->clazz = wave_core->object_class;
        obj->type = TYPE_STRING;
        return obj;
    }
    static WaveString* unbox(WaveObject* obj)
    {
        if(obj->type == TYPE_STRING)
            return static_cast<WaveString*>(obj->data);
        return nullptr;
    }
};
