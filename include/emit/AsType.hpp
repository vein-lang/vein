#pragma once
#include "WaveMember.hpp"
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"


WaveType* AsType(WaveClass* clazz)
{
    auto* parent = static_cast<WaveType*>(nullptr);
    if (clazz->Parent != nullptr)
        parent = AsType(clazz->Parent);
    auto result = reinterpret_cast<WaveType*>(
        new WaveTypeImpl(clazz->FullName, clazz->TypeCode, clazz->Flags, parent));

    result->Members->AddRange(clazz->Methods->Cast<WaveMember*>());
    result->Members->AddRange(clazz->Fields->Cast<WaveMember*>());

    return result;
}
