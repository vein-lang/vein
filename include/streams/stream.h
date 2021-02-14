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

class MemoryStream : Stream
{
public:
    MemoryStream() : MemoryStream(0) { }
    MemoryStream(int capacity)
    {
        _buffer = capacity != 0 ? new unsigned char[capacity] : new unsigned char[1];
        _capacity = capacity;
        _length = capacity;
    }

    MemoryStream(unsigned char* buffer, int size)
    {
        _buffer = buffer;
        _capacity = size;
        _length = size;
    }
    long Capacity() override
    {
        return _capacity - _origin;
    }
    long Length() override
    {
        return _length - _origin;
    }
    long Position() override
    {
        return _position - _origin;
    }
    int Read(unsigned char* buffer, const int offset, const int count)
    {
        auto n = _length - _position;
        if (n > count)
            n = count;
        if (n <= 0)
            return 0;
        auto byteCount = n;
        while (--byteCount >= 0)
            buffer[offset + byteCount] = _buffer[_position + byteCount];
        _position += n;
        return n;
    }
    unsigned char ReadByte()
    {
      auto* buffer = new unsigned char[1];
      return Read(buffer, 0, 1) == 0 ? -1 : buffer[0];
    }
private:
    unsigned char* _buffer;
    int _origin = 0;
    int _position = 0;
    int _length = 0;
    int _capacity = 0;
};

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

/*
 *public virtual int ReadByte()
    {
      byte[] buffer = new byte[1];
      return this.Read(buffer, 0, 1) == 0 ? -1 : (int) buffer[0];
    }
 */