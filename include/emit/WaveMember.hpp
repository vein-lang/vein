#pragma once
#include <string>

#include "WaveMemberKind.hpp"

class WaveMember
{
public:
    std::wstring Name;
    virtual WaveMemberKind GetKind() = 0;
protected:
    WaveMember(const std::wstring& name) { Name = name; }
};
