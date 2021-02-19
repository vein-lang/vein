#pragma once
#include <string>

#include "compatibility.types.hpp"
#include "utils/string.split.hpp"

using namespace std;

struct TypeName
{
    wstring FullName;
    [[nodiscard]] wstring get_name() const noexcept(false)
    {
        auto results = split(FullName, '/');
        auto result = results[results.size() - 1];
        results.clear();
        return result;
    }
    [[nodiscard]] wstring get_namespace() noexcept(false)
    {
        const auto n = get_name();
        return FullName.substr(0, (FullName.size() - n.size()) - 1);
    }
    TypeName(const wstring& n, const wstring& c) noexcept(true)
    {
        FullName = n + L"/" + c;
    }


    [[nodiscard]] static TypeName* construct(const int nameIdx, const int classIdx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new TypeName(m->operator()(nameIdx), m->operator()(classIdx));
    }
    [[nodiscard]] static TypeName* construct(const long idx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new TypeName(m->operator()(static_cast<int>(idx >> 32)), 
            m->operator()(static_cast<int>(idx & static_cast<uint32_t>(4294967295))));
    }
};
