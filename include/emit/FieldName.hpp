#pragma once
#include "compatibility.types.h"
#include "utils/string.split.h"
#include <string>

using namespace std;
struct FieldName
{
    string FullName;
    [[no_discard]] string GetName()
    {
        auto results = split(FullName, '.');
        auto result = results[results.size() - 1];
        results.clear();
        return result;
    }
    [[no_discard]] string GetClass()
    {
        auto n = GetName();
        return FullName.substr(0, (FullName.size() - n.size()) - 1);
    }

    FieldName(string n, string c)
    {
        FullName = n + "." + c;
    }


    [[no_discard]] static FieldName* Construct(int nameIdx, int classIdx, WaveModule* m)
    {
        return new FieldName(m->GetConstByIndex(nameIdx), m->GetConstByIndex(classIdx));
    }
    [[no_discard]] static FieldName* Construct(long idx, WaveModule* m)
    {
        return new FieldName(m->GetConstByIndex(static_cast<int>(idx >> 32)), 
            m->GetConstByIndex(static_cast<int>(idx & static_cast<uint32_t>(4294967295))));
    }
};