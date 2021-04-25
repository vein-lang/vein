#pragma once
#include <map>
#include <string>

#include "types/WaveString.hpp"
using namespace std;
static map<wstring, WaveString*>* string_intern_storage = nullptr;
class StringPool
{
public:
    static WaveString* Intern(const wstring& value)
    {
        if (string_intern_storage == nullptr)
            string_intern_storage = new map<wstring, WaveString*>();

        if (string_intern_storage->contains(value))
            return string_intern_storage->at(value);
        auto* str = new WaveString();
        str->SetValue(const_cast<wstring&>(value));
        string_intern_storage->insert({value, str});
        return str;
    }
};
