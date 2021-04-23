#pragma once
#include <string>


class WaveType;

class WaveArgumentRef
{
public:
    WaveType* Type;
    std::wstring Name;

    WaveArgumentRef() {  }
    /*WaveArgumentRef(const std::wstring& name, WaveType* type)
    {
        Name = name;
        Type = type;
    }*/
};
