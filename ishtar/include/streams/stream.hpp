#pragma once
#include "compatibility.types.hpp"

class Stream
{
public:
    virtual long Length() = 0;
    virtual long Position() = 0;
    virtual long Capacity() = 0;
};