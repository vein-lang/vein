#pragma once
#include "compatibility.types.h"
#include "utils/string.split.h"
#include <string>

using namespace std;
struct FieldName
{
    string FullName;
    [[nodiscard]] string get_name()
    {
        auto results = split(FullName, '.');
        auto result = results[results.size() - 1];
        results.clear();
        return result;
    }
    [[nodiscard]] string GetClass()
    {
        auto n = get_name();
        return FullName.substr(0, (FullName.size() - n.size()) - 1);
    }

    FieldName(const string& n, const string& c) noexcept(true)
    {
        FullName = n + "." + c;
    }
    [[nodiscard]] static FieldName* construct(const int nameIdx, const int classIdx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new FieldName(m(nameIdx), m(classIdx));
    }
    [[nodiscard]] static FieldName* construct(const long idx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new FieldName(m(static_cast<int>(idx >> 32)), 
            m(static_cast<int>(idx & static_cast<uint32_t>(4294967295))));
    }
};