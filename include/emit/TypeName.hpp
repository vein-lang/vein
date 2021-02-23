#pragma once
#include <string>
#include <fmt/format.h>
#include "compatibility.types.hpp"
#include "Exceptions.hpp"
#include "utils/string.split.hpp"
#include <map>

using namespace std;

struct TypeName;
static map<int64_t, TypeName*>* __TypeName_cache = nullptr;
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

    TypeName(const wstring& fullName) noexcept(true)
    {
        FullName = fullName;
    }

    [[nodiscard]] static TypeName* construct(const int nameIdx, const int classIdx, GetConstByIndexDelegate* m) noexcept(true)
    {
        return new TypeName(m->operator()(nameIdx), m->operator()(classIdx));
    }
    [[nodiscard]] static TypeName* construct(const int64_t idx, GetConstByIndexDelegate* m) noexcept(true)
    {
        if (__TypeName_cache == nullptr)
            __TypeName_cache = new map<int64_t, TypeName*>();
        if (__TypeName_cache->contains(idx))
            return __TypeName_cache->at(idx);

        auto result = new TypeName(m->operator()(static_cast<int>(idx & static_cast<uint32_t>(4294967295))), 
            m->operator()(static_cast<int>(idx >> 32)));

        __TypeName_cache->insert({idx, result});
        return result;
    }

    static void Validate(TypeName* name) noexcept(false)
    {
        if (!name->FullName.starts_with(L"global::"))
            throw InvalidFormatException(fmt::format(L"TypeName '{0}' has invalid. [name is not start with global::]", name->FullName));
    }
};
template<> struct equality<TypeName*> {
	static bool equal(TypeName* l, TypeName* r) {
		return wcscmp(l->FullName.c_str(), r->FullName.c_str()) == 0;
	}
};

template <> struct fmt::formatter<TypeName>: formatter<string_view> {
  template <typename FormatContext>
  auto format(TypeName c, FormatContext& ctx) {
    return formatter<string_view>::format(c.FullName, ctx);
  }
};
template <> struct fmt::formatter<TypeName*>: formatter<string_view> {
  template <typename FormatContext>
  auto format(TypeName* c, FormatContext& ctx) {
    return formatter<string_view>::format(c->FullName, ctx);
  }
};