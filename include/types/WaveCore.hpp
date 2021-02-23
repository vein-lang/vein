#pragma once
#include "WaveClass.hpp"
#include "emit/WaveModue.hpp"

typedef struct
{
    WaveModule* corlib;
    WaveClass* object_class;
    WaveClass* i1_class;
    WaveClass* i2_class;
    WaveClass* i4_class;
    WaveClass* i8_class;
    WaveClass* void_class;
    WaveClass* native_class;
    WaveClass* string_class;
    WaveClass* console_class;
    WaveClass* value_class;


    WaveType* object_type;
    WaveType* i4_type;
    WaveType* void_type;
    WaveType* value_type;
    WaveType* native_type;
    WaveType* string_type;
    WaveType* console_type;
} WaveCore;

static WaveCore* wave_core = new WaveCore();
