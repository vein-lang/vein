#pragma once
#include "memory_stream.hpp"


class BinaryReader
{
public:
    BinaryReader(MemoryStream origin)
    {
        _origin = origin;
    }

    unsigned int ReadU2()
    {
        return
            static_cast<unsigned int>(_origin.ReadByte()) << 0 | 
            static_cast<unsigned int>(_origin.ReadByte()) << 8;
    }
    int Read4()
    {
        return
            static_cast<unsigned char>(_origin.ReadByte()) << 0  | 
            static_cast<unsigned char>(_origin.ReadByte()) << 8  | 
            static_cast<unsigned char>(_origin.ReadByte()) << 16 | 
            static_cast<unsigned char>(_origin.ReadByte()) << 32;
    }
    long Read8()
    {
        return
            static_cast<unsigned char>(_origin.ReadByte()) << 0  | 
            static_cast<unsigned char>(_origin.ReadByte()) << 8  | 
            static_cast<unsigned char>(_origin.ReadByte()) << 16 | 
            static_cast<unsigned char>(_origin.ReadByte()) << 32 | 
            static_cast<unsigned char>(_origin.ReadByte()) << 40 | 
            static_cast<unsigned char>(_origin.ReadByte()) << 48 | 
            static_cast<unsigned char>(_origin.ReadByte()) << 64;
    }

private:
    MemoryStream _origin;
};
