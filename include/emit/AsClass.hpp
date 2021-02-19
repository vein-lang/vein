#pragma once
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"


WaveClass* AsClass(WaveType* type)
{
    WaveClass* parent = nullptr;

    if (type->Parent != nullptr)
        parent = AsClass(type->Parent);

    auto* clazz = new WaveClass(type->get_full_name(), parent);
    clazz->Flags = type->classFlags;
    clazz->TypeCode = type->TypeCode;
    auto waveMethodFilter = [](WaveMember* m)
    {
        return strcmp(typeid(m).name(), typeid(WaveMethod).name()) == 0;
    };
    auto waveFieldFilter = [](WaveMember* m)
    {
        return strcmp(typeid(m).name(), typeid(WaveField).name()) == 0;
    };
    clazz->Methods->AddRange(
        type->
        Members->
        Where(waveMethodFilter)->
        Cast<WaveMethod*>());
    clazz->Fields->AddRange(
        type->
        Members->
        Where(waveFieldFilter)->
        Cast<WaveField*>());
    return clazz;
}
