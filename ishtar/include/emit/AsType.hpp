#pragma once
#include "WaveMember.hpp"
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"

map<wstring, WaveType*>* __AsType_cache;
WaveType* AsType(WaveClass* clazz)
{
    if (__AsType_cache == nullptr)
        __AsType_cache = new map<wstring, WaveType*>();
    auto key = clazz->FullName->FullName;
    if (__AsType_cache->contains(key))
        return __AsType_cache->at(key);

    auto* parent = static_cast<WaveType*>(nullptr);
    if (clazz->Parent != nullptr)
        parent = AsType(clazz->Parent);
    auto result = reinterpret_cast<WaveType*>(
        new WaveTypeImpl(clazz->FullName, clazz->TypeCode, clazz->Flags, parent));

    result->Members->AddRange(clazz->Methods->Cast<WaveMember*>());
    result->Members->AddRange(clazz->Fields->Cast<WaveMember*>());

    __AsType_cache->insert({key, result});
    return result;
}
