#pragma once
#include <string>

#include "WaveMemberKind.hpp"

class WaveMember
{
public:
    std::string Name;
    virtual WaveMemberKind GetKind() = 0;
protected:
    WaveMember(const std::string& name) { Name = name; }
};
