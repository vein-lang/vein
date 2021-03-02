#pragma once
#include <string>
#include <fmt/format.h>
#include "compatibility.types.hpp"
#include "utils/string.split.hpp"
#include "utils/string.replace.hpp"
#include <map>


using namespace std;

struct TypeName;
static map<tuple<int, int, int>, TypeName*>* __TypeName_cache = nullptr;

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
    [[nodiscard]] wstring get_namespace() const noexcept(false)
    {
        auto rd = split(FullName, '%');
        auto& target = rd.at(1);
        return replace_string(target, get_name(), L"");
    }

    [[nodiscard]] wstring get_assembly_name() const noexcept(false)
    {
        auto splited = split(FullName, '%');
        return splited.at(0);
    }
    TypeName(const wstring& a, const wstring& n, const wstring& c) noexcept(true)
    {
        FullName = a + L"%" + c + L"/" + n;
    }

    TypeName(const wstring& fullName) noexcept(true)
    {
        FullName = fullName;
    }

    [[nodiscard]] static TypeName* construct(
        const int asmIdx,
        const int nameIdx, 
        const int namespaceIdx, 
        GetConstByIndexDelegate* m) noexcept(true)
    {
        if (__TypeName_cache == nullptr)
            __TypeName_cache = new map<tuple<int, int, int>, TypeName*>();
        auto key = make_tuple(asmIdx, nameIdx, namespaceIdx);
        if (__TypeName_cache->contains(key))
            return __TypeName_cache->at(key);

        auto* result = new TypeName(m->operator()(asmIdx), m->operator()(nameIdx), m->operator()(namespaceIdx));

        __TypeName_cache->insert({key, result});
        return result;
    }

    static void Validate(TypeName* name) noexcept(false)
    {
        //if (!name->FullName.starts_with(L"global::"))
        //    throw InvalidFormatException(fmt::format(L"TypeName '{0}' has invalid. [name is not start with global::]", name->FullName));
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


TypeName* readTypeName(uint32_t** ip, GetConstByIndexDelegate* m)
{
    ++(*ip);
    const auto asmIdx = READ32((*ip));
    ++(*ip);
    const auto nameIdx = READ32((*ip));
    ++(*ip);
    const auto nsIdx = READ32((*ip));
    ++(*ip);
    return TypeName::construct(asmIdx, nameIdx, nsIdx, m);
}