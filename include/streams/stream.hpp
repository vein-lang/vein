#pragma once
#include <cstring>
#include "compatibility.types.h"

class Stream
{
public:
    virtual long Length() = 0;
    virtual long Position() = 0;
    virtual long Capacity() = 0;
};