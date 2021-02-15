#pragma once
#include <map>
#include <string>
#include <unordered_map>

#include <fmt/core.h>

#include "Exceptions.hpp"
#include "types/WaveClass.h"
#include "types/WaveType.h"
#include "api/boolinq.h"
#include "collections/dictionary.hpp"

using namespace std;




struct WaveModule
{
    const char* name;
    dictionary<int, string>* strings;
    dictionary<string, WaveClass*>* classList;
    string GetConstByIndex(int index)
    {
        if (strings->contains(index))
            return strings->at(index);
        throw AggregateException(fmt::format("Index '{0}' not found in module '{1}'.", index, name));
    }

    WaveModule(const char* Name)
    {
        strings = new dictionary<int, string>;
        classList = new dictionary<string, WaveClass*>;
        name = Name;
    }
    
    WaveType* FindType(TypeName* type, bool findExternally = false) noexcept(false)
    {
        //bool filter(InsomniaClass x) => x!.FullName.Equals(type);
        if (!findExternally)
        {
            for(const auto & [ key, value ] : *classList)
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