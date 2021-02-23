#pragma once
#include <map>
#include <string>
#include <unordered_map>
#include "Exceptions.hpp"
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"
#include "collections/dictionary.hpp"
#include "emit/AsType.hpp"
#include "eq.hpp"
#include <fmt/format.h>
using namespace std;




struct WaveModule
{
    wstring name;
    dictionary<int, wstring>* strings;
    list_t<WaveClass*>* classList;
    list_t<WaveModule*>* deps;

    [[nodiscard]]
    wstring GetConstByIndex(int index) const noexcept(false)
    {
        if (strings->contains(index))
            return strings->at(index);
        throw AggregateException(fmt::format(L"Index '{0}' not found in module '{1}'.", index, name));
    }
    [[nodiscard]]
    int32_t GetStringConstant(const wstring& str) const noexcept(false)
    {
        if (str.empty())
            throw ArgumentNullException(L"GetStringConstant:: [str is empty]");
        auto key = static_cast<int32_t>(hash_gen<wstring>::getHashCode(str));
        if (!strings->contains(key))
            strings->insert({key, str});
        if (!equality<wstring>::equal(strings->at(key), str))
            throw CollisionDetectedException(fmt::format(
                L"Detected collisions of string constant. '{0}' and '{1}'.\n Please report this issue into {2}.",
                str, strings->at(key),
                L"https://github.com/0xF6/wave_lang/issues"));
        return key;
    }
    int64_t GetTypeConstant(TypeName* name) const noexcept(false)
    {
        const auto i1 = GetStringConstant(name->get_namespace());
        const auto i2 = GetStringConstant(name->get_name());
        int64_t b = i2;
        b <<= 32;
        b |= static_cast<uint32_t>(i1);
        return b;
    }
    int64_t GetTypeConstant(FieldName* name) const noexcept(false)
    {
        const auto i1 = GetStringConstant(name->GetClass());
        const auto i2 = GetStringConstant(name->get_name());
        int64_t b = i2;
        b <<= 32;
        b |= static_cast<uint32_t>(i1);
        return b;
    }


    WaveModule(const wstring Name)
    {
        strings = new dictionary<int, wstring>;
        classList = new list_t<WaveClass*>;
        deps = new list_t<WaveModule*>;
        name = Name;
    }
    
    WaveType* FindType(TypeName* type, bool findExternally = false) const noexcept(false)
    {
        const function<bool(WaveClass* z)> filter = [type](WaveClass* s) {
            return equality<TypeName*>::equal(type, s->FullName);
        };
        if (!findExternally)
            return AsType(classList->First(filter));
        auto* result = classList->FirstOrDefault(filter);
        if (result != nullptr)
            return AsType(result);
        
        for(auto* m : *deps)
        {
            auto* result = m->FindType(type, findExternally);
            if (result != nullptr)
                return result;
        }
        throw TypeNotFoundException(fmt::format(L"'{0}' not found in modules and dependency assemblies.", type->FullName));
    }

    WaveClass* FindClass(TypeName* type, bool findExternally = false) const noexcept(false)
    {
        const function<bool(WaveClass* z)> filter = [type](WaveClass* s) {
            return equality<TypeName*>::equal(type, s->FullName);
        };
        if (!findExternally)
            return classList->First(filter);
        auto* result = classList->FirstOrDefault(filter);
        if (result != nullptr)
            return result;
        
        for(auto* m : *deps)
        {
            auto* result = m->FindClass(type, findExternally);
            if (result != nullptr)
                return result;
        }
        throw TypeNotFoundException(fmt::format(L"'{0}' not found in modules and dependency assemblies.", type->FullName));
    }

    WaveMethod* GetMethod(const int tokenIdx, const int64_t ownerIdx)
    {
        function<wstring(int z)> get_const_string = [this](const int w) {
            return this->GetConstByIndex(w);
        };

        auto* clazzType = TypeName::construct(ownerIdx, &get_const_string);
        auto* clazz = FindClass(clazzType, true);

        return clazz->FindMethod(this->GetConstByIndex(tokenIdx));
    }

    WaveMethod* GetEntryPoint() const
    {
        for(auto* c : *classList)
        {
            for(auto* m : *c->Methods)
            {
                if (m->Name._Equal(L"master"))
                    return m;
            }
        }
        throw EntryPointNotFoundException();
    }
};