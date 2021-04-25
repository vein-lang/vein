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


class ConstStorage
{
    
};

struct WaveModule
{
    wstring name;
    
    list_t<WaveClass*>* class_table;
    list_t<WaveModule*>* deps;

    wstring* VERSION;

    dictionary<int, wstring>* strings_table;
    dictionary<int, TypeName*>* types_table;
    dictionary<int, FieldName*>* fields_table;

    ConstStorage* const_table;

    WaveModule(const wstring Name)
    {
        strings_table = new dictionary<int, wstring>;
        types_table = new dictionary<int, TypeName*>;
        fields_table = new dictionary<int, FieldName*>;
        class_table = new list_t<WaveClass*>;
        deps = new list_t<WaveModule*>;
        const_table = new ConstStorage();
        name = Name;
    }

    [[nodiscard]]
    wstring GetConstByIndex(int index) const noexcept(false)
    {
        if (strings_table->contains(index))
            return strings_table->at(index);
        throw AggregateException(fmt::format(L"Index '{0}' not found in module '{1}'.", index, name));
    }
    [[nodiscard]]
    wstring* GetRefConstByIndex(int index) const noexcept(false)
    {
        if (strings_table->contains(index))
            return &strings_table->at(index);
        throw AggregateException(fmt::format(L"Index '{0}' not found in module '{1}'.", index, name));
    }
    int32_t GetStringConstant(const wstring& str) const noexcept(false)
    {
        if (str.empty())
            throw ArgumentNullException(L"GetStringConstant:: [str is empty]");
        auto key = static_cast<int32_t>(hash_gen<wstring>::getHashCode(str));
        if (!strings_table->contains(key))
            strings_table->insert({key, str});
        if (!equality<wstring>::equal(strings_table->at(key), str))
            throw CollisionDetectedException(fmt::format(
                L"Detected collisions of string constant. '{0}' and '{1}'.\n Please report this issue into {2}.",
                str, strings_table->at(key),
                L"https://github.com/0xF6/wave_lang/issues"));
        return key;
    }
    void StageTypeConstant(TypeName* name) const noexcept(false)
    {
        GetStringConstant(name->get_assembly_name());
        GetStringConstant(name->get_namespace());
        GetStringConstant(name->get_name());
    }
    int64_t GetFieldConstant(FieldName* name) const noexcept(false)
    {
        const auto i1 = GetStringConstant(name->GetClass());
        const auto i2 = GetStringConstant(name->get_name());
        int64_t b = i2;
        b <<= 32;
        b |= static_cast<uint32_t>(i1);
        return b;
    }
    
    WaveType* FindType(TypeName* type, bool findExternally = false) const noexcept(false)
    {
        const function<bool(WaveClass* z)> filter = [type](WaveClass* s) {
            return equality<TypeName*>::equal(type, s->FullName);
        };
        if (!findExternally)
            return AsType(class_table->First(filter));
        auto* result = class_table->FirstOrDefault(filter);
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
            return class_table->First(filter);
        auto* result = class_table->FirstOrDefault(filter);
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
    WaveMethod* GetMethod(const int tokenIdx, const tuple<int, int, int> owner)
    {
        return GetMethod(tokenIdx, std::get<0>(owner), std::get<1>(owner), std::get<2>(owner));
    }
    WaveMethod* GetMethod(const int tokenIdx, const int asmIdx, const int nameIdx, const int nsIdx)
    {
        function<wstring(int z)> get_const_string = [this](const int w) {
            return this->GetConstByIndex(w);
        };

        auto* clazzType = TypeName::construct(asmIdx, nameIdx, nsIdx, &get_const_string);
        
        return GetMethod(tokenIdx, clazzType);
    }
    [[nodiscard]]
    WaveMethod* GetMethod(const int tokenIdx, TypeName* clazzType) const noexcept(false)
    {
        auto* clazz = FindClass(clazzType, true);
        return clazz->FindMethod(this->GetConstByIndex(tokenIdx));
    }
    [[nodiscard]]
    WaveMethod* GetEntryPoint() const
    {
        for(auto* c : *class_table)
        {
            for(auto* m : *c->Methods)
            {
                if (m->Name == L"master()")
                    return m;
            }
        }
        throw EntryPointNotFoundException();
    }
};