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
using namespace std;




struct WaveModule
{
    wstring name;
    dictionary<int, wstring>* strings;
    list_t<WaveClass*>* classList;
    list_t<WaveModule*>* deps;


    wstring GetConstByIndex(int index) const noexcept(false)
    {
        if (strings->contains(index))
            return strings->at(index);
        throw AggregateException("Index '"+to_string(index)+"' not found in module ''.");
    }
    [[nodiscard]]
    int32_t GetStringConstant(const wstring& str) const noexcept(false)
    {
        if (str.empty())
            throw ArgumentNullException("GetStringConstant:: [str is empty]");
        auto key = static_cast<int32_t>(hash_gen<wstring>::getHashCode(str));
        if (!strings->contains(key))
            strings->insert({key, str});
        if (!equality<wstring>::equal(strings->at(key), str))
            throw CollisionDetectedException("Detected collisions of string constant. '{str}' and '{strings[key]}'.\n Please report this issue into https://github.com/0xF6/wave_lang/issues.");
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
    
    WaveType* FindType(TypeName* type, bool findExternally = false) noexcept(false)
    {
        function<bool(WaveClass* z)> filter = [type](WaveClass* s) {
            return equality<TypeName*>::equal(type, s->FullName);
        };
        if (!findExternally)
            return AsType(classList->First(filter));
        auto* result = classList->FirstOrDefault(filter);
        if (result != nullptr)
            return AsType(result);
        
        for(auto m : *deps)
        {
            auto result = m->FindType(type, findExternally);
            if (result != nullptr)
                return result;
        }

        throw TypeNotFoundException("'{type}' not found in modules and dependency assemblies.");
    }
};