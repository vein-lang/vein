#pragma once
#include <map>

#include "list_t.hpp"
#include "emit/WaveArgumentRef.hpp"
#include "emit/WaveModue.hpp"

[[nodiscard]]
list_t<WaveArgumentRef*>* convert_map2list_args(const map<wstring, wstring>* args, WaveModule* m) noexcept(false)
{
    auto* list = new list_t<WaveArgumentRef*>();
    for(auto [key, value] : *args)
    {
       if (key == L"" && value == L"")
           continue;

        auto* arg = new WaveArgumentRef();
        auto* name = new TypeName(value);

        TypeName::Validate(name);

        arg->Name = key;
        arg->Type = m->FindType(new TypeName(value));
        list->push_back(arg);
    }
    return list;
}
