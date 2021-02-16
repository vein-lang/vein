#pragma once
#include <string>

#include "compatibility.types.hpp"
#include "utils/string.split.hpp"

using namespace std;

struct TypeName
{
    string FullName;
    [[nodiscard]] string get_name() noexcept(false)
    {
        auto results = split(FullName, '/');
        auto result = results[results.size() - 1];
        results.clear();
        return result;
    }
    [[nodiscard]] string get_namespace() noexcept(false)
    {
        auto n = get_name();
        return FullName.substr(0, (FullName.size() - n.size()) - 1);
    }
    TypeName(const string& n, const string& c) noexcept(true)
    {
        FullName = n + "/" + c;
    }


    [[nodiscard]] static TypeName* construct(const int nameIdx, const int classIdx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new TypeName(m(nameIdx), m(classIdx));
    }
    [[nodiscard]] static TypeName* construct(const long idx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new TypeName(m(static_cast<int>(idx >> 32)), 
            m(static_cast<int>(idx & static_cast<uint32_t>(4294967295))));
    }
};
