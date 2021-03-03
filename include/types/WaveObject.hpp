#pragma once
#include "compatibility.types.hpp"
#include "WaveClass.hpp"

struct WaveObject
{
	WaveClass*   clazz;
	WaveTypeCode type;
	void** vtable;


    WaveObject(WaveClass* _clazz)
    {
        type = _clazz->TypeCode;
        clazz = _clazz;
        vtable = new void*[_clazz->computed_size];
        memset(vtable, 0, sizeof(void*)*_clazz->computed_size );
    }
};
