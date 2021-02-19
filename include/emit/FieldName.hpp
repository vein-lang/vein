#pragma once
#include "compatibility.types.hpp"
#include "utils/string.split.hpp"
#include <string>

using namespace std;
struct FieldName
{
    wstring FullName;
    [[nodiscard]] wstring get_name() const
    {
        auto results = split(FullName, '.');
        auto result = results[results.size() - 1];
        results.clear();
        return result;
    }
    [[nodiscard]] wstring GetClass() const
    {
        auto n = get_name();
        return FullName.substr(0, (FullName.size() - n.size()) - 1);
    }

    FieldName(const wstring& n, const wstring& c) noexcept(true)
    {
        FullName = n + L"." + c;
    }
    [[nodiscard]] static FieldName* construct(const int nameIdx, const int classIdx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new FieldName(m->operator()(nameIdx), m->operator()(classIdx));
    }
    [[nodiscard]] static FieldName* construct(const long idx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new FieldName(m->operator()(static_cast<int>(idx >> 32)), 
            m->operator()(static_cast<int>(idx & static_cast<uint32_t>(4294967295))));
    }
};