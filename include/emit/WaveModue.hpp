#pragma once
#include <map>
#include <string>
#include <unordered_map>
#include "Exceptions.hpp"
#include "types/WaveClass.hpp"
#include "types/WaveType.hpp"
#include "collections/dictionary.hpp"

using namespace std;




struct WaveModule
{
    wstring name;
    dictionary<int, wstring>* strings;
    list_t<WaveClass*>* classList;
    wstring GetConstByIndex(int index)
    {
        if (strings->contains(index))
            return strings->at(index);
        throw AggregateException("Index '"+to_string(index)+"' not found in module ''.");
    }

    WaveModule(const wstring Name)
    {
        strings = new dictionary<int, wstring>;
        classList = new list_t<WaveClass*>;
        name = Name;
    }
    
    WaveType* FindType(TypeName* type, bool findExternally = false) noexcept(false)
    {
        //bool filter(InsomniaClass x) => x!.FullName.Equals(type);
        if (!findExternally)
        {
            for(const auto value : *classList)
            {
                //if (value->name == type->FullName)
                    //return (void*)value;
            }
        }

        //auto result = classList.FirstOrDefault(filter)?.AsType();
        //if (result is not null)
        //    return result;
        
        //foreach (var module in Deps)
        //{
        //    result = module.FindType(type, true);
        //    if (result is not null)
        //        return result;
        //}

        throw TypeNotFoundException("'{type}' not found in modules and dependency assemblies.");
    }
};