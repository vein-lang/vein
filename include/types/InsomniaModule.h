#pragma once
#include <map>
#include <compatibility.types.h>
#include <string>
#include "api/boolinq.h"
using namespace std;
struct InsomniaClass;
struct WaveType;
struct TypeName;



CUSTOM_EXCEPTION(AggregateException);
CUSTOM_EXCEPTION(TypeNotFoundException);

struct InsomniaModule
{
public:
    const char* name;
    map<int, string> strings;
    map<string, InsomniaClass*> classList;

    InsomniaModule(const char* Name) { name = Name;  }


    string GetConstByIndex(int index)
    {
        if (strings.contains(index))
            return strings[index];
        throw AggregateException("Index '%d' not found in module '%d'.");
    }

    WaveType* FindType(TypeName* type, bool findExternally = false)
    {
        //bool filter(InsomniaClass x) => x!.FullName.Equals(type);
        if (!findExternally)
        {
            for(auto s : classList)
            {
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

        throw new TypeNotFoundException("'{type}' not found in modules and dependency assemblies.");
    }
};
