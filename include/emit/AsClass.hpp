#pragma once
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"

map<wstring, WaveClass*>* __AsClass_cache;
WaveClass* AsClass(WaveType* type)
{
    if (__AsClass_cache == nullptr)
        __AsClass_cache = new map<wstring, WaveClass*>();
    auto key = type->get_full_name()->FullName;
    if (__AsClass_cache->contains(key))
        return __AsClass_cache->at(key);

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
    __AsClass_cache->insert({key, clazz});
    return clazz;
}
