#pragma once
#include "compatibility.types.hpp"
#include "eq.hpp"
#include "hash.hpp"


struct WaveString : public WaveObject
{
public:
    WaveString() : WaveObject(wave_core->string_class)
    {
    }

    [[nodiscard]]
    wstring* GetValue() const
    {
        return &reinterpret_cast<wstring&>(vtable[0]);
    }

    void SetValue(wstring& value) const
    {
        vtable[0] = static_cast<void*>(&value);
    }
};

template<> struct equality<WaveString>
{
	static bool equal(const WaveString* l, const WaveString* r)
    {
        auto* const l1 = l->vtable[0];
        if (l1 == nullptr)
            return false;
        auto* const r1 = r->vtable[0];
        if (r1 == nullptr)
            return false;

		return equality<wstring>::equal(
            static_cast<wstring*>(l1), 
            static_cast<wstring*>(r1));
	}
};

template<> struct hash_gen<WaveString>
{
    static size_t getHashCode(WaveString* s)
    {
        auto* const v = s->vtable[0];
        if (v == nullptr)
            return 0;
        return hash_gen<wchar_t*>::getHashCode(static_cast<wstring*>(v)->c_str());
    }
};