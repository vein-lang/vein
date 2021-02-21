#pragma once
#include <string>
#include <fmt/format.h>
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
        return new TypeName(m->operator()(static_cast<int>(idx & static_cast<uint32_t>(4294967295))), 
            m->operator()(static_cast<int>(idx >> 32)));
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