#pragma once
#include "types/WaveCore.h"
#include "types/WaveObject.h"
#include "types/WaveString.h"



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

template<> class WaveConvert<byte> {
public:
    static WaveObject* box(byte s) {
        auto* obj = new WaveObject();
        obj->data = reinterpret_cast<byte*>(s);
        obj->clazz = wave_core->i2_class;
        obj->type = TYPE_I2;
        return obj;
    }
    static byte unbox(WaveObject* obj)
    {
        if(obj->type == TYPE_STRING)
            return reinterpret_cast<byte>(obj->data);
        return 0; // TODO new fail
    }
};